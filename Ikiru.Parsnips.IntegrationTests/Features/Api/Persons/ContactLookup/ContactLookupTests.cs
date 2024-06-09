using System;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;
using Ikiru.Parsnips.IntegrationTests.Helpers;
using Ikiru.Parsnips.IntegrationTests.Helpers.Data;
using Ikiru.Parsnips.IntegrationTests.Helpers.External;
using Ikiru.Parsnips.IntegrationTests.Helpers.HttpMessages;
using Ikiru.Parsnips.Domain;
using Xunit;
using Xunit.Abstractions;

namespace Ikiru.Parsnips.IntegrationTests.Features.Api.Persons.ContactLookup
{
    [Collection(nameof(IntegrationTestCollection))]
    public class ContactLookupTests : IntegrationTestBase, IClassFixture<ContactLookupTests.ContactLookupTestsClassFixture>
    {
        private readonly ContactLookupTestsClassFixture m_ClassFixture;

        public ContactLookupTests(IntegrationTestFixture fixture, ContactLookupTestsClassFixture classFixture, ITestOutputHelper output) : base(fixture, output)
        {
            m_ClassFixture = classFixture;
        }

        public sealed class ContactLookupTestsClassFixture : IDisposable
        {
            public IntTestServer Server { get; }

            // ReSharper disable once UnusedParameter.Local
            public ContactLookupTestsClassFixture()
            {
                Server = new TestServerBuilder()
                    .AddSingleton(FakeRocketReachApi.RocketReachApi.Object)
                    .Build();
            }

            public void Dispose()
            {
                Server.Dispose();
            }
        }

        private Person m_Person;

        private async Task AddPersonIntoCosmos()
        {
            m_Person = new Person(m_ClassFixture.Server.Authentication.DefaultSearchFirmId, null, "Https://linkedin.com/in/testuser1111111111")
            {
                Name = "johnooa Testsq"
            };

            m_Person = await m_ClassFixture.Server.InsertItemIntoCosmos(TestDataManipulator.PersonsContainerName, m_Person.SearchFirmId, m_Person);
        }

        [Fact]
        public async Task PersonContactLookup()
        {
            await AddPersonIntoCosmos();

            var response = await m_ClassFixture.Server.Client.GetAsync($"/api/persons/{m_Person.Id}/contactlookup");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);

            var r = new
            {
                PersonId = Guid.Empty,
                EmailTeasers = Array.Empty<string>(),
                PhoneTeasers = Array.Empty<string>()
            };

            var responseJson = await response.Content.DeserializeToAnonymousType(r);

            Assert.Equal(m_Person.Id, responseJson.PersonId);
            Assert.NotEmpty(responseJson.EmailTeasers);
            await DestroyCosmosData();

        }

        private async Task DestroyCosmosData()
        {
            await m_ClassFixture.Server.RemoveItemFromCosmos<Person>(TestDataManipulator.PersonsContainerName, m_Person.SearchFirmId, c => c.Id == m_Person.Id);
        }
    }
}

