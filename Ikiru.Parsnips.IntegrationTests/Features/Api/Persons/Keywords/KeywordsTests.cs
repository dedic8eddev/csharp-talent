using System;
using System.Net;
using System.Threading.Tasks;
using Ikiru.Parsnips.IntegrationTests.Helpers;
using Ikiru.Parsnips.IntegrationTests.Helpers.Data;
using Ikiru.Parsnips.IntegrationTests.Helpers.HttpMessages;
using Ikiru.Parsnips.Domain;
using Xunit;
using Xunit.Abstractions;

namespace Ikiru.Parsnips.IntegrationTests.Features.Api.Persons.Keywords
{
    [Collection(nameof(IntegrationTestCollection))]
    public class KeywordsTests : IntegrationTestBase, IClassFixture<KeywordsTests.KeywordsTestsClassFixture>
    {
        private readonly KeywordsTestsClassFixture m_ClassFixture;

        public KeywordsTests(IntegrationTestFixture fixture, KeywordsTestsClassFixture classFixture, ITestOutputHelper output) : base(fixture, output)
        {
            m_ClassFixture = classFixture;
        }

        public class KeywordsTestsClassFixture : IDisposable
        {
            public IntTestServer Server { get; }

            // ReSharper disable once UnusedParameter.Local
            public KeywordsTestsClassFixture(IntegrationTestFixture fixture)
            {
                Server = new TestServerBuilder()
                   .Build();
            }

            public void Dispose()
            {
                Server.Dispose();
            }
        }

        private Person m_Person;

        private async Task EnsurePersonExists()
        {
            const string linkedInProfileId = "keyword-person";
            m_Person = new Person(m_ClassFixture.Server.Authentication.DefaultSearchFirmId, Guid.NewGuid(), $"https://uk.linkedin.com/in/{linkedInProfileId}")
            {
                Name = "Keyword Person"
            };

            m_Person = await m_ClassFixture.Server.AddUniqueItemIntoCosmos(TestDataManipulator.PersonsContainerName, m_Person.SearchFirmId, c => c.LinkedInProfileId == linkedInProfileId, m_Person);
        }

        [Fact]
        public async Task PostShouldRespondWithNoContent()
        {
            // GIVEN
            await EnsurePersonExists();
            var command = new { keyword = "sports person" };

            // WHEN
            var response = await m_ClassFixture.Server.Client.PostAsync($"/api/persons/{m_Person.Id}/keywords", new JsonContent(command));

            // THEN
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task DeleteShouldRespondWithNoContent()
        {
            // GIVEN
            await EnsurePersonExists();
            var command = new { keyword = "sports person" };
            var postResponse = await m_ClassFixture.Server.Client.PostAsync($"/api/persons/{m_Person.Id}/keywords", new JsonContent(command));
            Assert.Equal(HttpStatusCode.NoContent, postResponse.StatusCode);

            // WHEN
            var deleteResponse = await m_ClassFixture.Server.Client.DeleteAsync($"/api/persons/{m_Person.Id}/keywords/sports person");

            // THEN
            Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        }
    }
}