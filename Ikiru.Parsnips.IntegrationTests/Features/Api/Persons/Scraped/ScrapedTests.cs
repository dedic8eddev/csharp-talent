using Ikiru.Parsnips.IntegrationTests.Helpers;
using Ikiru.Parsnips.IntegrationTests.Helpers.Data;
using Ikiru.Parsnips.IntegrationTests.Helpers.External;
using Ikiru.Parsnips.IntegrationTests.Helpers.HttpMessages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Ikiru.Parsnips.IntegrationTests.Features.Api.Persons.Scraped
{
    [Collection(nameof(IntegrationTestCollection))]
    public class ScrapedTests : IntegrationTestBase, IClassFixture<ScrapedTests.ScrapedTestsClassFixture>
    {
        private readonly ScrapedTestsClassFixture m_ClassFixture;
        private static readonly Guid DataPoolPersonId = Guid.NewGuid();
        public sealed class ScrapedTestsClassFixture : IDisposable
        {
            public IntTestServer Server { get; }

            public ScrapedTestsClassFixture()
            {

                Server = new TestServerBuilder()
                    .AddSingleton(FakeDatapoolApi.SetupRefactor(ScrapedTests.DataPoolPersonId).Object)
                   .Build();
            }

            public void Dispose()
            {
                Server.Dispose();
            }
        }

        private Ikiru.Parsnips.Domain.Person m_Person;
        private Ikiru.Parsnips.Domain.Assignment m_Assignment;
        private Ikiru.Parsnips.Domain.Candidate m_Candidate;
        private Ikiru.Parsnips.Domain.Note m_Note;

        private readonly string m_LinkedInProfileId = $"int-test-person{Guid.NewGuid()}22";
        private string LinkedInProfileUrl => $"https://uk.linkedin.com/in/{m_LinkedInProfileId}";

        public ScrapedTests(IntegrationTestFixture fixture, ScrapedTestsClassFixture classFixture, ITestOutputHelper output) : base(fixture, output)
        {
            m_ClassFixture = classFixture;
        }

        private async Task SetupCosmosData()
        {
            m_Person = new Ikiru.Parsnips.Domain.Person(m_ClassFixture.Server.Authentication.DefaultSearchFirmId, Guid.NewGuid(), LinkedInProfileUrl)
            {
                DataPoolPersonId = ScrapedTests.DataPoolPersonId,
                Name = "IntTest Person1111111111111111111111111",
                JobTitle = "big cheese",
                SectorsIds = new List<string> { "I12691" },
                Location = "Fleet",
                TaggedEmails = new List<Ikiru.Parsnips.Domain.TaggedEmail> { new Ikiru.Parsnips.Domain.TaggedEmail { Email = "person.subj@integrationtests.com" }, new Ikiru.Parsnips.Domain.TaggedEmail { Email = "person.subj@inttests.com" } },
                PhoneNumbers = new List<string> { "01252 123456", "00000 333333" },
                Organisation = "Vegetably tomatoes",
                GdprLawfulBasisState = new Ikiru.Parsnips.Domain.PersonGdprLawfulBasisState
                {
                    GdprDataOrigin = "Some bloke",
                    GdprLawfulBasisOptionsStatus = Ikiru.Parsnips.Domain.Enums.GdprLawfulBasisOptionsStatusEnum.ConsentGiven,
                    GdprLawfulBasisOption = Ikiru.Parsnips.Domain.Enums.GdprLawfulBasisOptionEnum.DigitalConsent
                },


                Keywords = new List<string> { "Close to city centre", "Affordable price" },
                WebSites = new List<Ikiru.Parsnips.Domain.PersonWebsite>
                           {
                               new Ikiru.Parsnips.Domain.PersonWebsite { Url = "https://googleplus.com/profiles/talentis", Type = Ikiru.Parsnips.Domain.Enums.WebSiteType.Unknown }
                           }
            };

            m_Person = await m_ClassFixture.Server.InsertItemIntoCosmos<Ikiru.Parsnips.Domain.Person>(TestDataManipulator.PersonsContainerName, m_Person.SearchFirmId, m_Person);

            m_Assignment = new Ikiru.Parsnips.Domain.Assignment(m_ClassFixture.Server.Authentication.DefaultSearchFirmId)
            {
                Name = "Test Assign21",
                Status = Ikiru.Parsnips.Domain.Enums.AssignmentStatus.Active,
                JobTitle = "asdfdasfdsa",
                StartDate = DateTimeOffset.Now.AddDays(100)
            };
            m_Assignment = await m_ClassFixture.Server.InsertItemIntoCosmos(TestDataManipulator.AssignmentsContainerName, m_Assignment.SearchFirmId, m_Assignment);

            m_Candidate = new Ikiru.Parsnips.Domain.Candidate(m_ClassFixture.Server.Authentication.DefaultSearchFirmId, m_Assignment.Id, m_Person.Id)
            {
                InterviewProgressState = new Ikiru.Parsnips.Domain.InterviewProgress
                {
                    Status = Ikiru.Parsnips.Domain.Enums.CandidateStatusEnum.LeftMessage,
                    Stage = Ikiru.Parsnips.Domain.Enums.CandidateStageEnum.FirstClientInterview
                }
            };
            m_Candidate = await m_ClassFixture.Server.InsertItemIntoCosmos(TestDataManipulator.CandidateContainerName, m_Candidate.SearchFirmId, m_Candidate);

            m_Note = new Ikiru.Parsnips.Domain.Note(m_Person.Id, m_ClassFixture.Server.Authentication.DefaultUserId, m_ClassFixture.Server.Authentication.DefaultSearchFirmId)
            {
                NoteTitle = "nt 1",
                AssignmentId = m_Assignment.Id
            };
            m_Note = await m_ClassFixture.Server.InsertItemIntoCosmos(TestDataManipulator.PersonNotes, m_Note.SearchFirmId, m_Note);
        }

        // Bug :  when a test fails this isnt called.
        private async Task DestroyCosmosData()
        {
            await m_ClassFixture.Server.RemoveItemFromCosmos<Ikiru.Parsnips.Domain.Person>(TestDataManipulator.PersonsContainerName, m_Candidate.SearchFirmId, c => c.Id == m_Person.Id);
            await m_ClassFixture.Server.RemoveItemFromCosmos<Ikiru.Parsnips.Domain.Assignment>(TestDataManipulator.PersonsContainerName, m_Candidate.SearchFirmId, c => c.Id == m_Assignment.Id);
            await m_ClassFixture.Server.RemoveItemFromCosmos<Ikiru.Parsnips.Domain.Candidate>(TestDataManipulator.PersonsContainerName, m_Candidate.SearchFirmId, c => c.Id == m_Candidate.Id);
            await m_ClassFixture.Server.RemoveItemFromCosmos<Ikiru.Parsnips.Domain.Note>(TestDataManipulator.PersonsContainerName, m_Candidate.SearchFirmId, c => c.Id == m_Note.Id);
        }

        [Fact]
        public async Task PostScrapedShouldRespondCreated()
        {
            // Given
            await SetupCosmosData();
            await m_ClassFixture.Server.RemoveItemFromCosmos<Ikiru.Parsnips.Domain.Person>(TestDataManipulator.PersonsContainerName, m_ClassFixture.Server.Authentication.DefaultSearchFirmId, c => c.LinkedInProfileId == "hannibal_lecter");

            var command = new
            {
                scrapeOriginatorType = "LinkedInSearch",
                scrapeOriginatorUrl = "https://www.linkedin.com/search/results/all/?keywords=James%20Wilson&origin=GLOBAL_SEARCH_HEADER",
                data = new
                {
                    identifier = m_Person.LinkedInProfileUrl,
                    avatar = "...message is too long for teams to send...",
                    name = FakeDatapoolApi.RefactorStubData.First().PersonDetails.Name,
                    currentRole = FakeDatapoolApi.RefactorStubData.First().CurrentEmployment.Position,
                    location = "Salisbury"
                }
            };

            // When

            var response = await m_ClassFixture.Server.Client.PostAsync("/api/persons/PersonsScraped", new Ikiru.Parsnips.IntegrationTests.Helpers.HttpMessages.JsonContent(command));

            var result = response.Content.ReadAsStringAsync().Result;

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);


            var r = new
            {

                LocalPerson = new
                {
                    PersonId = (Guid?) Guid.Empty,
                    DataPoolId = (Guid?) Guid.Empty,
                    Name = "",
                    CompanyName = "",
                    Location = "",
                    JobTitle = "",
                    CurrentSectors = new string[] { },
                    RecentAssignment = new
                    {
                        Name = "",
                        Stage = "",
                        Status = ""
                    },
                    RecentNote = new
                    {
                        NoteTitle = "",
                        ByFirstName = "",
                        ByLastName = "",
                        CreatedOrUpdated = DateTime.MinValue
                    },
                    Websites = new[]
                        {
                            new {
                                    WebsiteType = "",
                                    Url = ""
                                }
                        },
                    Photo = new
                    {
                        Url = ""
                    }
                },
                DataPoolPerson = new
                {
                    PersonId = (Guid?) Guid.Empty,
                    DataPoolId = (Guid?) Guid.Empty,
                    Name = "",
                    CompanyName = "",
                    Location = "",
                    JobTitle = "",
                    CurrentSectors = new string[] { },
                    RecentAssignment = new
                    {
                        Name = "",
                        Stage = "",
                        Status = ""
                    },
                    RecentNote = new
                    {
                        NoteTitle = "",
                        ByFirstName = "",
                        ByLastName = "",
                        CreatedOrUpdated = DateTime.MinValue
                    },
                    Websites = new[]
                        {
                            new {
                                    WebsiteType = "",
                                    Url = ""
                                }
                        },
                    Photo = new
                    {
                        Url = ""
                    }
                }
            };

            var responseJson = await response.Content.DeserializeToAnonymousType(r);
            Assert.NotNull(responseJson);
            Assert.Equal(command.data.name, responseJson.DataPoolPerson.Name);
            Assert.NotEmpty(responseJson.DataPoolPerson.Websites);
            Assert.NotEmpty(responseJson.DataPoolPerson.Location);
            Assert.NotEmpty(responseJson.DataPoolPerson.JobTitle);
            Assert.NotEmpty(responseJson.DataPoolPerson.CompanyName);

            Assert.NotEmpty(responseJson.LocalPerson.Websites);
            Assert.NotNull(responseJson.LocalPerson.RecentNote);
            Assert.NotEmpty(responseJson.LocalPerson.Name);
            Assert.NotEmpty(responseJson.LocalPerson.Location);
            Assert.NotEmpty(responseJson.LocalPerson.JobTitle);
            Assert.NotEmpty(responseJson.LocalPerson.CompanyName);

            await DestroyCosmosData();
        }

    }
}
