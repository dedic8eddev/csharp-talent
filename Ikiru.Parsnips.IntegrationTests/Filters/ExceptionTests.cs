using Ikiru.Parsnips.IntegrationTests.Helpers;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Ikiru.Parsnips.IntegrationTests.Helpers.Data;
using Ikiru.Parsnips.IntegrationTests.Helpers.HttpMessages;
using Ikiru.Parsnips.Domain;
using Xunit;
using Xunit.Abstractions;

namespace Ikiru.Parsnips.IntegrationTests.Filters
{
    /// <summary>
    /// Tests for ensuring Exception Filters and return codes are hooked up correctly.
    /// </summary>
    [Collection(nameof(IntegrationTestCollection))]
    public class ExceptionTests : IntegrationTestBase, IClassFixture<ExceptionTests.ExceptionTestsClassFixture>
    {
        private readonly ExceptionTestsClassFixture m_ClassFixture;

        public sealed class ExceptionTestsClassFixture : IDisposable
        {
            public IntTestServer Server { get; }

            public ExceptionTestsClassFixture()
            {
                Server = new TestServerBuilder()
                   .Build();
            }

            public void Dispose()
            {
                Server.Dispose();
            }
        }


        public ExceptionTests(IntegrationTestFixture fixture, ExceptionTestsClassFixture classFixture, ITestOutputHelper output)
            : base(fixture, output)
        {
            m_ClassFixture = classFixture;
        }

        [Fact]
        public async Task EndpointRespondsWithNotFoundResult()
        {
            // Given
            var notExistsPersonId = Guid.NewGuid();

            // When
            var response = await m_ClassFixture.Server.Client.GetAsync($"/api/persons/{notExistsPersonId}");

            // Then
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            Assert.Equal("application/problem+json", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);

            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.NotNull(responseContent);

            using var document = JsonDocument.Parse(responseContent);

            var root = document.RootElement;
            Assert.Equal("https://tools.ietf.org/html/rfc7231#section-6.5.4", root.GetProperty("type").GetString());
            Assert.Equal("Resource not found", root.GetProperty("title").GetString());
            Assert.Equal((int)HttpStatusCode.NotFound, root.GetProperty("status").GetInt32());

            Assert.Equal($"Unable to find 'Person' with Id '{notExistsPersonId}'", root.GetProperty("errors").GetProperty("Person")[0].GetString());
        }
        
        [Fact]
        public async Task EndpointRespondWithBadRequestResultFromFluentValidation()
        {
            // Given
            const string missingParameterValuesUrl = "/api/persons?linkedInProfileUrl=";
            // When
            var response = await m_ClassFixture.Server.Client.GetAsync(missingParameterValuesUrl);

            // Then
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            Assert.Equal("application/problem+json", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);

            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.NotNull(responseContent);

            using var document = JsonDocument.Parse(responseContent);

            var root = document.RootElement;
            Assert.Equal("https://tools.ietf.org/html/rfc7231#section-6.5.1", root.GetProperty("type").GetString());
            Assert.Equal("One or more validation errors occurred.", root.GetProperty("title").GetString());
            Assert.Equal((int)HttpStatusCode.BadRequest, root.GetProperty("status").GetInt32());

            Assert.Equal("'Linked In Profile Url' must not be empty.", root.GetProperty("errors").GetProperty("LinkedInProfileUrl")[0].GetString());
        }
        
        [Fact]
        public async Task EndpointRespondWithBadRequestResultNotFromFluentValidation()
        {
            // Given
            const string linkedInProfileId = "corona_virus";
            var linkedInProfileUrl = $"https://uk.linkedin.com/in/{linkedInProfileId}";
            await m_ClassFixture.Server.AddUniqueItemIntoCosmos(TestDataManipulator.PersonsContainerName, m_ClassFixture.Server.Authentication.DefaultSearchFirmId, c => c.LinkedInProfileId == linkedInProfileId, 
                                                                new Person(m_ClassFixture.Server.Authentication.DefaultSearchFirmId, null, linkedInProfileUrl));

            var command = new
                          {
                              Name = "Corona Virus",
                              LinkedInProfileUrl = linkedInProfileUrl
                          };

            // When
            var response = await m_ClassFixture.Server.Client.PostAsync("/api/persons", new JsonContent(command));
            
            // Then
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            Assert.Equal("application/problem+json", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);

            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.NotNull(responseContent);

            using var document = JsonDocument.Parse(responseContent);

            var root = document.RootElement;
            Assert.Equal("https://tools.ietf.org/html/rfc7231#section-6.5.1", root.GetProperty("type").GetString());
            Assert.Equal("One or more validation errors occurred.", root.GetProperty("title").GetString());
            Assert.Equal((int)HttpStatusCode.BadRequest, root.GetProperty("status").GetInt32());

            Assert.Equal("A record already exists with this LinkedInProfileUrl", root.GetProperty("errors").GetProperty("LinkedInProfileUrl")[0].GetString());
        }
    }
}
