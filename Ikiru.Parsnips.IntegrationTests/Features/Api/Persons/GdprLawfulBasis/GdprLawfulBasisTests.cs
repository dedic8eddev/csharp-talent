using Ikiru.Parsnips.IntegrationTests.Helpers;
using Ikiru.Parsnips.Domain;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Ikiru.Parsnips.IntegrationTests.Helpers.Data;
using Ikiru.Parsnips.IntegrationTests.Helpers.HttpMessages;
using Xunit;
using Xunit.Abstractions;

namespace Ikiru.Parsnips.IntegrationTests.Features.Api.Persons.GdprLawfulBasis
{
    [Collection(nameof(IntegrationTestCollection))]
    public class GdprLawfulBasisTests : IntegrationTestBase, IClassFixture<GdprLawfulBasisTests.GdprLawfulBasisTestsClassFixture>
    {
        private readonly GdprLawfulBasisTestsClassFixture m_ClassFixture;

        public sealed class GdprLawfulBasisTestsClassFixture : IDisposable
        {
            public IntTestServer Server { get; }

            public GdprLawfulBasisTestsClassFixture()
            {
                Server = new TestServerBuilder()
                   .Build();
            }

            public void Dispose()
            {
                Server.Dispose();
            }
        }

        public GdprLawfulBasisTests(IntegrationTestFixture fixture, GdprLawfulBasisTestsClassFixture classFixture, ITestOutputHelper output) : base(fixture, output)
        {
            m_ClassFixture = classFixture;
        }

        [Fact]
        public async Task PutShouldRespondNoContent()
        {
            // Given
            await m_ClassFixture.Server.RemoveItemFromCosmos<Person>(TestDataManipulator.PersonsContainerName, m_ClassFixture.Server.Authentication.DefaultSearchFirmId, c => c.LinkedInProfileId == "hannibal_lecter");

            var postCommand = new
            {
                Name = "Hannibal Lecter",
                JobTitle = "Chef",
                Location = "Basingstoke, Hampshire",
                EmailAddresses = new List<string> { "eatwell@silence.lambs" },
                PhoneNumbers = new List<string> { "09876 543210" },
                Company = "Red Dragon Ltd.",
                LinkedInProfileUrl = "https://uk.linkedin.com/in/hannibal_lecter"
            };


            var postResponse = await m_ClassFixture.Server.Client.PostAsync("/api/persons", new JsonContent(postCommand));
            var personId = (await postResponse.Content.DeserializeToAnonymousType(new { Id = Guid.Empty })).Id;

            // When
            
            var putCommand = new
                             {
                                 GdprLawfulBasisState = new
                                                        {
                                                            gdprLawfulBasisOptionsStatus = "consentRequestSent",
                                                            gdprLawfulBasisOption = "emailConsent",
                                                            gdprDataOrigin = "data in the public access"
                                                        }
                             };

            var putResponse = await m_ClassFixture.Server.Client.PutAsync($"/api/persons/{personId}/gdprlawfulbasis", new JsonContent(putCommand));

            // Then
            Assert.Equal(HttpStatusCode.NoContent, putResponse.StatusCode);
        }
    }
}