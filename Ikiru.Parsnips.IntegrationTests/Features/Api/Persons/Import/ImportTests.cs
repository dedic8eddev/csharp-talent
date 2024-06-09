using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Ikiru.Parsnips.IntegrationTests.Helpers;
using Ikiru.Parsnips.IntegrationTests.Helpers.HttpMessages;
using Xunit;
using Xunit.Abstractions;

namespace Ikiru.Parsnips.IntegrationTests.Features.Api.Persons.Import
{
    [Collection(nameof(IntegrationTestCollection))]
    public class ImportTests : IntegrationTestBase, IClassFixture<ImportTests.ImportTestsClassFixture>
    {
        private readonly ImportTestsClassFixture m_ClassFixture;

        public ImportTests(IntegrationTestFixture fixture, ImportTestsClassFixture classFixture, ITestOutputHelper output) : base(fixture, output)
        {
            m_ClassFixture = classFixture;
        }

        public sealed class ImportTestsClassFixture : IDisposable
        {
            public IntTestServer Server { get; }

            // ReSharper disable once UnusedParameter.Local
            public ImportTestsClassFixture()
            {
                Server = new TestServerBuilder()
                   .Build();
            }

            public void Dispose()
            {
                Server.Dispose();
            }
        }

        [Fact]
        public async Task PostShouldRespondWithOk()
        {
            // GIVEN
            const string filePath = @".\Features\Api\Persons\Import\sample.profile.json";
            const string linkedInProfileId = "raymond-parlour";
            var linkedInProfileUrl = $"https://uk.linkedin.com/in/{linkedInProfileId}";

            var content = CreateMultipartFileUploadContent(filePath, linkedInProfileUrl, "application/json");

            // WHEN
            var response = await m_ClassFixture.Server.Client.PostAsync("/api/persons/import", content);

            // THEN
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);

            var r = new
            {
                Id = Guid.Empty
            };
            var responseJson = await response.Content.DeserializeToAnonymousType(r);
            Assert.NotNull(responseJson);
            Assert.NotEqual(Guid.Empty, responseJson.Id);
        }

        [Fact]
        public async Task GetShouldRespondWithNoContent()
        {
            // GIVEN
            const string filePath = @".\Features\Api\Persons\Import\doc.pdf";
            const string linkedInProfileId = "raymond-parlour-existing";
            var linkedInProfileUrl = $"https://uk.linkedin.com/in/{linkedInProfileId}";

            var content = CreateMultipartFileUploadContent(filePath, linkedInProfileUrl, "application/pdf");
            var postResponse = await m_ClassFixture.Server.Client.PostAsync("/api/persons/import", content);

            Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

            // WHEN 
            var response = await m_ClassFixture.Server.Client.GetAsync($"/api/persons/import/?linkedInProfileUrl={WebUtility.UrlEncode(linkedInProfileUrl)}");

            // THEN
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        private static MultipartFormDataContent CreateMultipartFileUploadContent(string filePath, string fileName, string fileContentType)
        {
            var content = new MultipartFormDataContent();

            using var fs = File.OpenRead(filePath);
            var streamContent = new StreamContent(fs);
            var imageContent = new ByteArrayContent(streamContent.ReadAsByteArrayAsync().Result);
            imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse(fileContentType);
            content.Add(imageContent, "file", fileName);

            return content;
        }
    }
}