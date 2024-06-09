using Ikiru.Parsnips.IntegrationTests.Helpers;
using Ikiru.Parsnips.IntegrationTests.Helpers.Data;
using Ikiru.Parsnips.IntegrationTests.Helpers.External;
using Ikiru.Parsnips.IntegrationTests.Helpers.HttpMessages;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Blobs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Ikiru.Parsnips.IntegrationTests.Features.Api.Persons
{
    [Collection(nameof(IntegrationTestCollection))]
    public class PersonsTests : IntegrationTestBase, IClassFixture<PersonsTests.PersonsTestsClassFixture>
    {
        private readonly PersonsTestsClassFixture m_ClassFixture;
        private static readonly Guid DataPoolPersonId = Guid.NewGuid();
        public sealed class PersonsTestsClassFixture : IDisposable
        {
            public IntTestServer Server { get; }

            public PersonsTestsClassFixture()
            {

                Server = new TestServerBuilder()
                    .AddSingleton(FakeDatapoolApi.SetupRefactor(PersonsTests.DataPoolPersonId).Object)
                    .AddSingleton(FakeDatapoolApi.Setup(PersonsTests.DataPoolPersonId).Object)
                   .Build();
            }

            public void Dispose()
            {
                Server.Dispose();
            }
        }

        private Person m_Person;
        private Assignment m_Assignment;
        private Candidate m_Candidate;
        private Note m_Note;
        private readonly string m_LinkedInProfileId = $"int-test-person{Guid.NewGuid()}2";
        private string LinkedInProfileUrl => $"https://uk.linkedin.com/in/{m_LinkedInProfileId}";

        public PersonsTests(IntegrationTestFixture fixture, PersonsTestsClassFixture classFixture, ITestOutputHelper output) : base(fixture, output)
        {
            m_ClassFixture = classFixture;
        }

        private async Task SetupCosmosData()
        {
            m_Person = new Person(m_ClassFixture.Server.Authentication.DefaultSearchFirmId, Guid.NewGuid(), LinkedInProfileUrl)
            {
                DataPoolPersonId = PersonsTests.DataPoolPersonId,
                Name = "IntTest Person111111111111111111111111",
                JobTitle = "big cheese",
                SectorsIds = new List<string> { "I12691" },
                Location = "Fleet",
                TaggedEmails = new List<TaggedEmail> { new TaggedEmail { Email = "person.subj@integrationtests.com" }, new TaggedEmail { Email = "person.subj@inttests.com" } },
                PhoneNumbers = new List<string> { "01252 123456", "00000 333333" },
                Organisation = "Vegetably tomatoes",
                GdprLawfulBasisState = new PersonGdprLawfulBasisState
                {
                    GdprDataOrigin = "Some bloke",
                    GdprLawfulBasisOptionsStatus = GdprLawfulBasisOptionsStatusEnum.ConsentGiven,
                    GdprLawfulBasisOption = GdprLawfulBasisOptionEnum.DigitalConsent
                },


                Keywords = new List<string> { "Close to city centre", "Affordable price" },
                WebSites = new List<PersonWebsite>
                           {
                               new PersonWebsite { Url = "https://googleplus.com/profiles/talentis", Type = WebSiteType.Unknown }
                           }
            };

            m_Person = await m_ClassFixture.Server.InsertItemIntoCosmos<Person>(TestDataManipulator.PersonsContainerName, m_Person.SearchFirmId, m_Person);


            m_Assignment = new Assignment(m_ClassFixture.Server.Authentication.DefaultSearchFirmId)
            {
                Name = "Test Assign21",
                Status = AssignmentStatus.Active,
                JobTitle = "asdfdasfdsa",
                StartDate = DateTimeOffset.Now.AddDays(100)
            };
            m_Assignment = await m_ClassFixture.Server.InsertItemIntoCosmos(TestDataManipulator.AssignmentsContainerName, m_Assignment.SearchFirmId, m_Assignment);

            m_Candidate = new Candidate(m_ClassFixture.Server.Authentication.DefaultSearchFirmId, m_Assignment.Id, m_Person.Id)
            {
                InterviewProgressState = new InterviewProgress
                {
                    Status = CandidateStatusEnum.LeftMessage,
                    Stage = CandidateStageEnum.FirstClientInterview
                }
            };
            m_Candidate = await m_ClassFixture.Server.InsertItemIntoCosmos(TestDataManipulator.CandidateContainerName, m_Candidate.SearchFirmId, m_Candidate);

            m_Note = new Note(m_Person.Id, m_ClassFixture.Server.Authentication.DefaultUserId, m_ClassFixture.Server.Authentication.DefaultSearchFirmId)
            {
                NoteTitle = "nt 1",
                AssignmentId = m_Assignment.Id
            };
            m_Note = await m_ClassFixture.Server.InsertItemIntoCosmos(TestDataManipulator.PersonNotes, m_Note.SearchFirmId, m_Note);
        }

        // Bug :  when a test fails this isnt called.
        private async Task DestroyCosmosData()
        {
            await m_ClassFixture.Server.RemoveItemFromCosmos<Person>(TestDataManipulator.PersonsContainerName, m_Candidate.SearchFirmId, c => c.Id == m_Person.Id);
            await m_ClassFixture.Server.RemoveItemFromCosmos<Assignment>(TestDataManipulator.PersonsContainerName, m_Candidate.SearchFirmId, c => c.Id == m_Assignment.Id);
            await m_ClassFixture.Server.RemoveItemFromCosmos<Candidate>(TestDataManipulator.PersonsContainerName, m_Candidate.SearchFirmId, c => c.Id == m_Candidate.Id);
            await m_ClassFixture.Server.RemoveItemFromCosmos<Note>(TestDataManipulator.PersonsContainerName, m_Candidate.SearchFirmId, c => c.Id == m_Note.Id);
        }

        private async Task UploadPhoto()
        {
            const string filePath = @".\Features\Api\Persons\picture.png";
            await using var fs = File.OpenRead(filePath);
            await m_ClassFixture.Server.GetContainer(BlobStorage.ContainerNames.PersonsDocuments)
                                .UploadBlobAsync($"{m_Person.SearchFirmId}/{m_Person.Id}/photo", fs);
        }

        [Fact]
        public async Task GetByIdShouldRespondOkWhenProfileIsFound()
        {
            // Given
            await SetupCosmosData();
            await UploadPhoto();

            // When
            var response = await m_ClassFixture.Server.Client.GetAsync($"/api/persons/{m_Person.Id}");

            // Then
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);

            var r = new
            {
                LocalPerson = new
                {
                    Id = Guid.Empty,
                    DataPoolId = Guid.Empty,
                    Name = "",
                    JobTitle = "",
                    Location = "",
                    TaggedEmails = new[]
                              {
                                  new
                                  {
                                      Email = "",
                                      SmtpValid = ""
                                  }
                              },
                    PhoneNumbers = new List<string>(),
                    Company = "",
                    LinkedInProfileUrl = "",
                    GdprLawfulBasisState = new
                    {
                        GdprDataOrigin = "",
                        GdprLawfulBasisOptionsStatus = "",
                        GdprLawfulBasisOption = ""
                    },
                    Keywords = new List<string>(),
                    Photo = new
                    {
                        Url = ""
                    },
                    WebSites = new[]
                           {
                               new
                               {
                                   Url = "",
                                   Type = ""
                               }
                           }
                },
                DataPoolPerson = new
                {
                    Id = Guid.Empty,
                    DataPoolId = Guid.Empty,
                    Name = "",
                    JobTitle = "",
                    Location = "",
                    TaggedEmails = new[]
                              {
                                  new
                                  {
                                      Email = "",
                                      SmtpValid = ""
                                  }
                              },
                    PhoneNumbers = new List<string>(),
                    Company = "",
                    LinkedInProfileUrl = "",
                    GdprLawfulBasisState = new
                    {
                        GdprDataOrigin = "",
                        GdprLawfulBasisOptionsStatus = "",
                        GdprLawfulBasisOption = ""
                    },
                    Keywords = new List<string>(),
                    Photo = new
                    {
                        Url = ""
                    },
                    WebSites = new[]
                           {
                               new
                               {
                                   Url = "",
                                   Type = ""
                               }
                           }
                }
            };

            var responseJson = await response.Content.DeserializeToAnonymousType(r);

            Assert.Equal(m_Person.Name, responseJson.LocalPerson.Name);
            Assert.Equal(m_Person.JobTitle, responseJson.LocalPerson.JobTitle);
            Assert.Equal(m_Person.Location, responseJson.LocalPerson.Location);
            Assert.All(m_Person.TaggedEmails, e => Assert.Contains(responseJson.LocalPerson.TaggedEmails, te => e.Email == te.Email && e.SmtpValid == te.SmtpValid));
            Assert.Equal(m_Person.PhoneNumbers, responseJson.LocalPerson.PhoneNumbers);
            Assert.Equal(m_Person.Organisation, responseJson.LocalPerson.Company);
            Assert.Equal(m_Person.LinkedInProfileUrl, responseJson.LocalPerson.LinkedInProfileUrl);
            Assert.NotNull(responseJson.LocalPerson.GdprLawfulBasisState);
            Assert.Equal(m_Person.GdprLawfulBasisState.GdprDataOrigin, responseJson.LocalPerson.GdprLawfulBasisState.GdprDataOrigin);
            Assert.Equal(m_Person.GdprLawfulBasisState.GdprLawfulBasisOptionsStatus.AsCamelCase(), responseJson.LocalPerson.GdprLawfulBasisState.GdprLawfulBasisOptionsStatus);
            Assert.Equal(m_Person.GdprLawfulBasisState.GdprLawfulBasisOption.AsCamelCase(), responseJson.LocalPerson.GdprLawfulBasisState.GdprLawfulBasisOption);
            Assert.Equal(m_Person.Keywords, responseJson.LocalPerson.Keywords);
            Assert.Equal(m_Person.WebSites[0].Url, responseJson.LocalPerson.WebSites[0].Url);
            Assert.Equal("other", responseJson.LocalPerson.WebSites[0].Type);
            Assert.StartsWith($"http://127.0.0.1:10000/devstoreaccount1/{BlobStorage.ContainerNames.PersonsDocuments}/{m_Person.SearchFirmId}/{m_Person.Id}/photo?", responseJson.LocalPerson.Photo.Url);

            Assert.Equal(DataPoolPersonId, responseJson.DataPoolPerson.DataPoolId);
            Assert.Equal(FakeDatapoolApi.StubData[0].PersonDetails.Name, responseJson.DataPoolPerson.Name);
            await DestroyCosmosData();
        }

        [Fact]
        public async Task GetListShouldRespondOk()
        {
            // Given
            await SetupCosmosData();

            // When 
            var response = await m_ClassFixture.Server.Client.GetAsync($"/api/persons?linkedInProfileUrl={WebUtility.UrlEncode(LinkedInProfileUrl)}");

            // Then
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);

            var r = new
            {
                Persons = new[]
                                  {
                                      new {
                                          LocalPerson = new
                                                          {
                                                              Id = Guid.Empty,
                                                              DataPoolId = Guid.Empty,
                                                              Name = "",
                                                              JobTitle = "",
                                                              Location = "",
                                                              TaggedEmails = new []
                                                                             {
                                                                                 new
                                                                                 {
                                                                                     Email = "",
                                                                                     SmtpValid = ""
                                                                                 }
                                                                             },
                                                              PhoneNumbers = new List<string>(),
                                                              Company = "",
                                                              LinkedInProfileUrl = "",
                                                              GdprLawfulBasisState = new
                                                                                     {
                                                                                         GdprDataOrigin = "",
                                                                                         GdprLawfulBasisOptionsStatus = "",
                                                                                         GdprLawfulBasisOption = ""
                                                                                     },
                                                              WebSites = new []
                                                                         {
                                                                             new
                                                                             {
                                                                                 Url = "",
                                                                                 Type = ""
                                                                             }
                                                                         }
                                                          },
                                                DataPoolPerson = new
                                                          {
                                                              Id = Guid.Empty,
                                                              DataPoolId = Guid.Empty,
                                                              Name = "",
                                                              JobTitle = "",
                                                              Location = "",
                                                              TaggedEmails = new []
                                                                             {
                                                                                 new
                                                                                 {
                                                                                     Email = "",
                                                                                     SmtpValid = ""
                                                                                 }
                                                                             },
                                                              PhoneNumbers = new List<string>(),
                                                              Company = "",
                                                              LinkedInProfileUrl = "",
                                                              GdprLawfulBasisState = new
                                                                                     {
                                                                                         GdprDataOrigin = "",
                                                                                         GdprLawfulBasisOptionsStatus = "",
                                                                                         GdprLawfulBasisOption = ""
                                                                                     },
                                                              WebSites = new []
                                                                         {
                                                                             new
                                                                             {
                                                                                 Url = "",
                                                                                 Type = ""
                                                                             }
                                                                         }
                                                          }
                                      }
                                  }
            };
            var responseJson = await response.Content.DeserializeToAnonymousType(r);
            Assert.NotNull(responseJson);
            Assert.NotNull(responseJson.Persons);
            var person = Assert.Single(responseJson.Persons);
            Assert.NotNull(person);
            Assert.Equal(m_Person.Id, person.LocalPerson.Id);
            Assert.Equal(m_Person.Name, person.LocalPerson.Name);
            Assert.Equal(m_Person.JobTitle, person.LocalPerson.JobTitle);
            Assert.Equal(m_Person.Location, person.LocalPerson.Location);
            Assert.All(m_Person.TaggedEmails, e => Assert.Contains(person.LocalPerson.TaggedEmails, te => e.Email == te.Email && e.SmtpValid == te.SmtpValid));
            Assert.Equal(m_Person.PhoneNumbers, person.LocalPerson.PhoneNumbers);
            Assert.Equal(m_Person.Organisation, person.LocalPerson.Company);
            Assert.Equal(m_Person.LinkedInProfileUrl, person.LocalPerson.LinkedInProfileUrl);
            Assert.Equal(m_Person.WebSites[0].Url, person.LocalPerson.WebSites[0].Url);
            Assert.Equal("other", person.LocalPerson.WebSites[0].Type);

            Assert.Equal(DataPoolPersonId, person.DataPoolPerson.DataPoolId);
            await DestroyCosmosData();
        }

        [Fact]
        public async Task GetByWebsiteUrl()
        {
            // Given
            await SetupCosmosData();
            await UploadPhoto();

            // When
            var response = await m_ClassFixture.Server.Client.GetAsync($"/api/persons/GetByWebsiteUrl?WebsiteUrl={LinkedInProfileUrl}");

            // Then
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);

            var responseJson = await response.Content.ReadAsStringAsync();
            Newtonsoft.Json.Linq.JObject responseObject = Newtonsoft.Json.Linq.JObject.Parse(responseJson);

            Assert.Equal(m_Person.Name, responseObject["localPerson"]["name"]);
            Assert.Equal(m_Person.JobTitle, responseObject["localPerson"]["jobTitle"]);
            Assert.Equal(m_Person.Location, responseObject["localPerson"]["location"]);
            Assert.Equal(m_Person.TaggedEmails[0].Email, responseObject["localPerson"]["taggedEmails"][0]["email"]);
            //Assert.Equal(m_Person.PhoneNumbers, responseObject["localPerson"]["phoneNumbers"].ToList<string>());
            Assert.Equal(m_Person.Organisation, responseObject["localPerson"]["company"]);
            Assert.Equal(m_Person.LinkedInProfileUrl, responseObject["localPerson"]["linkedInProfileUrl"]);
            Assert.Equal(m_Person.GdprLawfulBasisState.GdprDataOrigin, responseObject["localPerson"]["gdprLawfulBasisState"]["gdprDataOrigin"]);
            Assert.Equal(m_Person.GdprLawfulBasisState.GdprLawfulBasisOptionsStatus.AsCamelCase(), responseObject["localPerson"]["gdprLawfulBasisState"]["gdprLawfulBasisOptionsStatus"]);
            Assert.Equal(m_Person.GdprLawfulBasisState.GdprLawfulBasisOption.AsCamelCase(), responseObject["localPerson"]["gdprLawfulBasisState"]["gdprLawfulBasisOption"]);
            //Assert.Equal(m_Person.Keywords[0], responseObject["keywords"][0]);
            //Assert.Equal(m_Person.WebSites[0].Url, responseObject["localPerson"]["webSites"][0]["Url"]);
            Assert.Equal("other", responseObject["localPerson"]["webSites"][0]["type"].ToString());
            Assert.Equal(m_Person.DataPoolPersonId.ToString(), responseObject["localPerson"]["dataPoolId"]);
            Assert.Equal(m_Person.Id.ToString(), responseObject["localPerson"]["localId"].ToString());

            await DestroyCosmosData();
        }


        [Fact]
        public async Task GetPersonByWebsiteUrl()
        {
            // Given
            await SetupCosmosData();
            await UploadPhoto();

            // When
            var response = await m_ClassFixture.Server.Client.GetAsync($"/api/persons/GetPersonByWebsiteUrl?WebsiteUrl={LinkedInProfileUrl}");

            // Then
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);

            var responseJson = await response.Content.ReadAsStringAsync();
            Newtonsoft.Json.Linq.JObject responseObject = Newtonsoft.Json.Linq.JObject.Parse(responseJson);

            Assert.Equal(m_Person.Name, responseObject["localPerson"]["name"]);
            Assert.Equal(m_Person.JobTitle, responseObject["localPerson"]["jobTitle"]);
            Assert.Equal(m_Person.Location, responseObject["localPerson"]["location"]);
            Assert.Equal(m_Person.TaggedEmails[0].Email, responseObject["localPerson"]["taggedEmails"][0]["email"]);
            //Assert.Equal(m_Person.PhoneNumbers, responseObject["localPerson"]["phoneNumbers"].ToList<string>());
            Assert.Equal(m_Person.Organisation, responseObject["localPerson"]["companyName"]);
            Assert.Equal(m_Person.LinkedInProfileUrl, responseObject["localPerson"]["linkedInProfileUrl"]);
            Assert.Equal(m_Person.GdprLawfulBasisState.GdprDataOrigin, responseObject["localPerson"]["gdprLawfulBasisState"]["gdprDataOrigin"]);
            Assert.Equal(m_Person.GdprLawfulBasisState.GdprLawfulBasisOptionsStatus.AsCamelCase(), responseObject["localPerson"]["gdprLawfulBasisState"]["gdprLawfulBasisOptionsStatus"]);
            Assert.Equal(m_Person.GdprLawfulBasisState.GdprLawfulBasisOption.AsCamelCase(), responseObject["localPerson"]["gdprLawfulBasisState"]["gdprLawfulBasisOption"]);
            //Assert.Equal(m_Person.Keywords[0], responseObject["keywords"][0]);
            //Assert.Equal(m_Person.WebSites[0].Url, responseObject["localPerson"]["webSites"][0]["Url"]);
            Assert.Equal("other", responseObject["localPerson"]["websites"][0]["websiteType"].ToString());
            Assert.Equal(m_Person.DataPoolPersonId.ToString(), responseObject["localPerson"]["dataPoolId"]);
            Assert.Equal(m_Person.Id.ToString(), responseObject["localPerson"]["personId"].ToString());

            await DestroyCosmosData();
        }

        [Fact]
        public async Task GetPhotoUrlFromDataPool()
        {
            // Given
            await SetupCosmosData();

            // When
            var response = await m_ClassFixture.Server.Client.GetAsync($"/api/persons/{FakeDatapoolApi.PersonId}/getexternalphoto");

            // Then
            var r = new
            {
                Photo = new
                {
                    Url = ""
                }
            };

            var responseJson = await response.Content.DeserializeToAnonymousType(r);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);

            Assert.Equal(FakeDatapoolApi.PhotoUrl, responseJson.Photo.Url);

            await DestroyCosmosData();
        }

        [Fact]
        public async Task GetSimilar()
        {
            // Given
            await SetupCosmosData();

            // When
            var response = await m_ClassFixture.Server.Client.GetAsync($"/api/persons/{m_Person.Id}/GetSimilar");

            // Then
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);
        }

        [Fact]
        public async Task PostShouldRespondCreated()
        {
            // Given
            await SetupCosmosData();
            await m_ClassFixture.Server.RemoveItemFromCosmos<Person>(TestDataManipulator.PersonsContainerName, m_ClassFixture.Server.Authentication.DefaultSearchFirmId, c => c.LinkedInProfileId == "hannibal_lecter");

            var command = new
            {
                Name = "Hannibal Lecter",
                JobTitle = "Chef",
                Location = "Basingstoke, Hampshire",
                TaggedEmails = new[] { new { Email = "eatwell@silence.lambs", SmtpValid = "" }, new { Email = "lambs@silence.com", SmtpValid = "valid" } },
                PhoneNumbers = new List<string> { "09876 543210", "00000 333333" },
                Company = "Red Dragon Ltd.",
                LinkedInProfileUrl = "https://uk.linkedin.com/in/hannibal_lecter"
            };

            // When
            var response = await m_ClassFixture.Server.Client.PostAsync("/api/persons", new JsonContent(command));

            // Then
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);

            var r = new
            {
                Id = Guid.Empty,
                Name = "",
                JobTitle = "",
                Location = "",
                TaggedEmails = new[] { new { Email = "", SmtpValid = "" } },
                PhoneNumbers = new List<string>(),
                Company = "",
                LinkedInProfileUrl = ""
            };
            var responseJson = await response.Content.DeserializeToAnonymousType(r);
            Assert.NotNull(responseJson);

            Assert.Equal($"{m_ClassFixture.Server.Client.BaseAddress}api/Persons/{responseJson.Id}", response.Headers.Location.ToString());

            Assert.NotEqual(Guid.Empty, responseJson.Id);
            Assert.Equal(command.Name, responseJson.Name);
            Assert.Equal(command.JobTitle, responseJson.JobTitle);
            Assert.Equal(command.Location, responseJson.Location);
            Assert.All(command.TaggedEmails, e => Assert.Contains(responseJson.TaggedEmails, te => e.Email == te.Email && e.SmtpValid == te.SmtpValid));
            Assert.Equal(command.PhoneNumbers, responseJson.PhoneNumbers);
            Assert.Equal(command.Company, responseJson.Company);
            Assert.Equal(command.LinkedInProfileUrl, responseJson.LinkedInProfileUrl);

            await DestroyCosmosData();
        }


        [Fact]
        public async Task PostDataPoolLinkageShouldRespondCreated()
        {
            // Given
            await SetupCosmosData();

            var command = new
            {
                DataPoolPersonId = PersonsTests.DataPoolPersonId
            };

            // When
            var response = await m_ClassFixture.Server.Client.PostAsync("/api/persons/DataPoolLinkage", new JsonContent(command));

            // Then
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);

            var r = new
            {
                LocalPerson = new
                {
                    Id = Guid.Empty,
                    DataPoolPersonId = Guid.Empty
                },
                DataPoolPerson = new
                {
                    Id = Guid.Empty,
                }
            };
            var responseJson = await response.Content.DeserializeToAnonymousType(r);
            Assert.NotNull(responseJson);

            Assert.NotEqual(Guid.Empty, responseJson.LocalPerson.Id);
            Assert.NotEqual(Guid.Empty, responseJson.LocalPerson.DataPoolPersonId);

            await DestroyCosmosData();
        }


        [Fact]
        public async Task PutShouldRespondOk()
        {
            // Given
            await m_ClassFixture.Server.RemoveItemFromCosmos<Person>(TestDataManipulator.PersonsContainerName, m_ClassFixture.Server.Authentication.DefaultSearchFirmId, c => c.LinkedInProfileId == "hannibal_lecter");

            var postCommand = new
            {
                Name = "Hannibal Lecter",
                JobTitle = "Chef",
                Location = "Basingstoke, Hampshire",
                TaggedEmails = new[] { new { Email = "eatwell@silence.lambs", SmtpValid = "" } },
                PhoneNumbers = new List<string> { "09876 543210" },
                Company = "Red Dragon Ltd.",
                LinkedInProfileUrl = $"https://uk.linkedin.com/in/hannibal-lecter-{Guid.NewGuid()}"
            };

            var postResponse = await m_ClassFixture.Server.Client.PostAsync("/api/persons", new JsonContent(postCommand));
            var postResponseJson = await postResponse.Content.DeserializeToAnonymousType(new { Id = Guid.Empty });

            var command = new
            {
                Name = "Dr. Hannibal Lecter",
                JobTitle = "Chef",
                Location = "Basingstoke, Hampshire",
                TaggedEmails = new[] { new { Email = "dr@hannibal.lecter", SmtpValid = "" }, new { Email = "info@silence.com", SmtpValid = "valid" } },
                PhoneNumbers = new List<string> { "01234 567890", "00000 333333" },
                Company = "Red Dragon Ltd.",
                LinkedInProfileUrl = $"https://uk.linkedin.com/in/dr-hannibal-lecter-{Guid.NewGuid()}",
                WebSites = new[] { new { Url = "https://www.reuters.com/companies/talentis" } }
            };

            // When
            var response = await m_ClassFixture.Server.Client.PutAsync($"/api/persons/{postResponseJson.Id}", new JsonContent(command));

            // Then
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);

            var tmp = await response.Content.ReadAsStringAsync();

            var r = new
            {
                LocalPerson = new
                {
                    Id = Guid.Empty,
                    Name = "",
                    JobTitle = "",
                    Location = "",
                    TaggedEmails = new[] { new { Email = "", SmtpValid = "" } },
                    PhoneNumbers = new List<string>(),
                    Company = "",
                    LinkedInProfileUrl = "",
                    WebSites = new[]
                                   {
                                       new
                                       {
                                           Url = "",
                                           Type = ""
                                       }
                                   }
                }
            };
            var responseJson = (await response.Content.DeserializeToAnonymousType(r)).LocalPerson;

            Assert.NotNull(responseJson);

            Assert.Equal(postResponseJson.Id, responseJson.Id);
            Assert.Equal(command.Name, responseJson.Name);
            Assert.Equal(command.JobTitle, responseJson.JobTitle);
            Assert.Equal(command.Location, responseJson.Location);
            Assert.All(command.TaggedEmails, e => Assert.Contains(responseJson.TaggedEmails, te => e.Email == te.Email && e.SmtpValid == te.SmtpValid));
            Assert.Equal(command.PhoneNumbers, responseJson.PhoneNumbers);
            Assert.Equal(command.Company, responseJson.Company);
            Assert.Equal(command.LinkedInProfileUrl, responseJson.LinkedInProfileUrl);
            Assert.Equal(command.WebSites.First().Url, responseJson.WebSites.First(x => x.Type.ToLower() == "reuters").Url);
        }
    }
}
