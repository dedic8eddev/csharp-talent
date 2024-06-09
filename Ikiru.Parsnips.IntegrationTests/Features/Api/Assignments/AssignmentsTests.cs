using Ikiru.Parsnips.IntegrationTests.Helpers;
using Ikiru.Parsnips.Domain.Enums;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Ikiru.Parsnips.IntegrationTests.Helpers.Data;
using Ikiru.Parsnips.IntegrationTests.Helpers.HttpMessages;
using Ikiru.Parsnips.Domain;
using Xunit;
using Xunit.Abstractions;

namespace Ikiru.Parsnips.IntegrationTests.Features.Api.Assignments
{
    [Collection(nameof(IntegrationTestCollection))]
    public class AssignmentsTests : IntegrationTestBase, IClassFixture<AssignmentsTests.AssignmentsTestsClassFixture>
    {
        private readonly AssignmentsTestsClassFixture m_ClassFixture;
        private Assignment m_Assignment;
        private Assignment m_AssignmentB;
        private Person m_Person;
        private Candidate m_Candidate;

        public AssignmentsTests(IntegrationTestFixture fixture, AssignmentsTestsClassFixture classFixture, ITestOutputHelper output) : base(fixture, output)
        {
            m_ClassFixture = classFixture;
            EnsureDependentItemsInCosmos().GetAwaiter().GetResult();
        }

        public sealed class AssignmentsTestsClassFixture : IDisposable
        {
            public IntTestServer Server { get; }

            // ReSharper disable once UnusedParameter.Local
            public AssignmentsTestsClassFixture()
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
        public async Task PostShouldRespondWithCreatedAssignment()
        {
            // Given
            var command = new
            {
                Name = "Dream job: Work when sleep Inc - dream person",
                CompanyName = "Work when sleep Inc",
                JobTitle = "Dreamer",
                Location = "from home",
                StartDate = DateTimeOffset.Now.AddDays(3)
            };

            // When
            var response = await m_ClassFixture.Server.Client.PostAsync("/api/assignments", new JsonContent(command));

            // Then
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var r = new
            {
                Id = Guid.Empty,
                Name = "",
                CompanyName = "",
                JobTitle = "",
                Location = "",
                StartDate = DateTimeOffset.MinValue,
                Status = (AssignmentStatus?)null
            };

            // Then
            var responseJson = await response.Content.DeserializeToAnonymousType(r);
            Assert.NotNull(responseJson);

            Assert.NotEqual(Guid.Empty, responseJson.Id);
            Assert.Equal(command.Name, responseJson.Name);
            Assert.Equal(command.CompanyName, responseJson.CompanyName);
            Assert.Equal(command.JobTitle, responseJson.JobTitle);
            Assert.Equal(command.Location, responseJson.Location);
            Assert.Equal(command.StartDate, responseJson.StartDate);
            Assert.Equal(AssignmentStatus.Active, responseJson.Status);
        }

        [Fact]
        public async Task GetShouldRespondWithCorrectAssignment()
        {
            // Given
            var command = new
            {
                Name = "Ref 30317 - sw engineer - parsnips",
                CompanyName = "Fruity Parsnips",
                JobTitle = "software engineer",
                Location = "Basingstoke",
                StartDate = DateTimeOffset.Now.AddDays(7)
            };

            var postResponse = await m_ClassFixture.Server.Client.PostAsync("/api/assignments", new JsonContent(command));
            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

            var postResponseContent = await postResponse.Content.ReadAsStringAsync();
            var postDocument = JsonDocument.Parse(postResponseContent);

            var postRoot = postDocument.RootElement;
            var id = postRoot.GetProperty("id").GetString();

            // When
            var getResponse = await m_ClassFixture.Server.Client.GetAsync($"/api/assignments/{id}");

            // Then
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            var r = new
            {
                Id = Guid.Empty,
                Name = "",
                CompanyName = "",
                JobTitle = "",
                Location = "",
                StartDate = DateTimeOffset.MinValue,
                Status = (AssignmentStatus?)null
            };

            // Then
            var responseJson = await getResponse.Content.DeserializeToAnonymousType(r);
            Assert.NotNull(responseJson);

            Assert.NotEqual(Guid.Empty, responseJson.Id);
            Assert.Equal(command.Name, responseJson.Name);
            Assert.Equal(command.CompanyName, responseJson.CompanyName);
            Assert.Equal(command.JobTitle, responseJson.JobTitle);
            Assert.Equal(command.Location, responseJson.Location);
            Assert.Equal(command.StartDate, responseJson.StartDate);
            Assert.Equal(AssignmentStatus.Active, responseJson.Status);
        }

        [Fact]
        public async Task GetListShouldRespondWithCorrectAssignments()
        {           
            // When
            var response = await m_ClassFixture.Server.Client.GetAsync("/api/assignments/");

            // Then
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var r = new
            {
                Assignments = new[]
                                      {
                                          new
                                          {
                                              Id = Guid.Empty,
                                              Name = "",
                                              CompanyName = "",
                                              JobTitle = "",
                                              Location = "",
                                              StartDate = DateTimeOffset.MinValue,
                                              Status = (AssignmentStatus?)null,
                                              CandidateStageCount = new
                                                                    {
                                                                        Identified = 0,
                                                                        Screening = 0,
                                                                        InternalInterview = 0,
                                                                        ShortList = 0,
                                                                        FirstClientInterview = 0,
                                                                        SecondClientInterview = 0,
                                                                        ThirdClientInterview = 0,
                                                                        Offer = 0,
                                                                        Placed = 0,
                                                                        Archive = 0
                                                                    }
                                          }
                                      }
            };
            var responseJson = await response.Content.DeserializeToAnonymousType(r);
            Assert.NotNull(responseJson);
            Assert.NotNull(responseJson.Assignments);

            Assert.True(responseJson.Assignments.Length >= 2, "Expected at least 2 assignments");

            var firstAssignment = responseJson.Assignments[0];
            Assert.Equal(m_AssignmentB.Id, firstAssignment.Id);
            Assert.Equal(m_AssignmentB.Name, firstAssignment.Name);
            Assert.Equal(m_AssignmentB.CompanyName, firstAssignment.CompanyName);
            Assert.Equal(m_AssignmentB.JobTitle, firstAssignment.JobTitle);
            Assert.Equal(m_AssignmentB.Location, firstAssignment.Location);
            Assert.Equal(m_AssignmentB.StartDate, firstAssignment.StartDate);
            Assert.Equal(AssignmentStatus.Active, firstAssignment.Status);

            var secondAssignment = responseJson.Assignments[1];
            Assert.Equal(m_Assignment.Id, secondAssignment.Id);
            Assert.Equal(m_Assignment.Name, secondAssignment.Name);
            Assert.Equal(m_Assignment.CompanyName, secondAssignment.CompanyName);
            Assert.Equal(m_Assignment.JobTitle, secondAssignment.JobTitle);
            Assert.Equal(m_Assignment.Location, secondAssignment.Location);
            Assert.Equal(m_Assignment.StartDate, secondAssignment.StartDate);
            Assert.Equal(AssignmentStatus.Active, secondAssignment.Status);
            Assert.Equal(1, secondAssignment.CandidateStageCount.FirstClientInterview);
        }

        [Fact]
        public async Task GetSimpleListShouldRespondWithCorrectAssignments()
        {
            // When
            var response = await m_ClassFixture.Server.Client.GetAsync("/api/assignments/getsimplelist?totalitemcount=2");

            // Then
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var r = new
            {
                HasAssignments = (bool?)null,
                SimpleActiveAssignments = new[]
                                          {
                                              new
                                              {
                                                  Id = Guid.Empty,
                                                  Name = "",
                                                  CompanyName = "",
                                                  JobTitle = "",
                                                  Location = "",
                                                  StartDate = DateTimeOffset.MinValue
                                              }
                                          }
            };
            var responseJson = await response.Content.DeserializeToAnonymousType(r);
            Assert.NotNull(responseJson);
            Assert.NotNull(responseJson.SimpleActiveAssignments);

            Assert.Equal(2, responseJson.SimpleActiveAssignments.Length);

            var firstAssignment = responseJson.SimpleActiveAssignments[0];
            Assert.Equal(m_AssignmentB.Id, firstAssignment.Id);
            Assert.Equal(m_AssignmentB.Name, firstAssignment.Name);
            Assert.Equal(m_AssignmentB.CompanyName, firstAssignment.CompanyName);
            Assert.Equal(m_AssignmentB.JobTitle, firstAssignment.JobTitle);
            Assert.Equal(m_AssignmentB.Location, firstAssignment.Location);
            Assert.Equal(m_AssignmentB.StartDate, firstAssignment.StartDate);

            var secondAssignment = responseJson.SimpleActiveAssignments[1];
            Assert.Equal(m_Assignment.Id, secondAssignment.Id);
            Assert.Equal(m_Assignment.Name, secondAssignment.Name);
            Assert.Equal(m_Assignment.CompanyName, secondAssignment.CompanyName);
            Assert.Equal(m_Assignment.JobTitle, secondAssignment.JobTitle);
            Assert.Equal(m_Assignment.Location, secondAssignment.Location);
            Assert.Equal(m_Assignment.StartDate, secondAssignment.StartDate);
        }

        [Fact]
        public async Task PutShouldUpdateAssignmentandReturnValues()
        {
            // Given

            var postCommand = new
            {
                Name = "Name a",
                CompanyName = "Company a",
                JobTitle = "JobTitle a",
                Location = "Location a",
                StartDate = DateTimeOffset.Now.AddDays(3).ToOffset(new TimeSpan(-5, 0, 0))
            };

            var postResponse = await m_ClassFixture.Server.Client.PostAsync("/api/assignments", new JsonContent(postCommand));
            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            var assignmentId = (await postResponse.Content.DeserializeToAnonymousType(new { Id = Guid.Empty })).Id;

            var putCommand = new
            {
                Name = "Update Name a",
                CompanyName = "Update Company a",
                JobTitle = "Update JobTitle a",
                Location = "Update Location a",
                StartDate = DateTimeOffset.Now.AddDays(5).ToOffset(new TimeSpan(-5, 0, 0)),
                Status = AssignmentStatus.OnHold
            };

            // When
            var putResponse = await m_ClassFixture.Server.Client.PutAsync($"/api/assignments/{assignmentId}", new JsonContent(putCommand));
            Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

            var r = new
            {
                Id = Guid.Empty,
                Name = "",
                CompanyName = "",
                JobTitle = "",
                Location = "",
                StartDate = DateTimeOffset.MinValue,
                Status = (AssignmentStatus?)null,
            };

            // Then
            var responseJson = await putResponse.Content.DeserializeToAnonymousType(r);
            Assert.NotNull(responseJson);

            Assert.Equal(assignmentId, responseJson.Id);
            Assert.Equal(putCommand.Name, responseJson.Name);
            Assert.Equal(putCommand.CompanyName, responseJson.CompanyName);
            Assert.Equal(putCommand.JobTitle, responseJson.JobTitle);
            Assert.Equal(putCommand.Location, responseJson.Location);
            Assert.Equal(putCommand.StartDate, responseJson.StartDate);
            Assert.Equal(putCommand.Status, responseJson.Status);
        }

        private async Task EnsureDependentItemsInCosmos()
        {
            m_Assignment = new Assignment(m_ClassFixture.Server.Authentication.DefaultSearchFirmId)
            {
                Name = $"Get List A-{Guid.NewGuid()}",
                CompanyName = "Listing A Inc",
                JobTitle = "List A",
                Location = "Many A",
                StartDate = DateTimeOffset.Now.AddDays(3).ToOffset(new TimeSpan(-5, 0, 0))
            };
            m_Assignment = await m_ClassFixture.Server.AddUniqueItemIntoCosmos(TestDataManipulator.AssignmentsContainerName, m_Assignment.SearchFirmId, a => a.Name == m_Assignment.Name, m_Assignment);

            m_AssignmentB = new Assignment(m_ClassFixture.Server.Authentication.DefaultSearchFirmId)
            {
                Name = $"Get List B-{Guid.NewGuid()}",
                CompanyName = "Listing B Inc.",
                JobTitle = "List B",
                Location = "Many B",
                StartDate = DateTimeOffset.Now.AddDays(30)
            };
            m_AssignmentB = await m_ClassFixture.Server.AddUniqueItemIntoCosmos(TestDataManipulator.AssignmentsContainerName, m_AssignmentB.SearchFirmId, a => a.Name == m_AssignmentB.Name, m_AssignmentB);


            m_Person = new Person(m_ClassFixture.Server.Authentication.DefaultSearchFirmId)
            {
                Name = "Person for Candidate Tests v1",
                JobTitle = "A Job Title",
                Organisation = "A Company"
            };

            m_Person = await m_ClassFixture.Server.InsertItemIntoCosmos(TestDataManipulator.PersonsContainerName, m_Person.SearchFirmId, m_Person);

            m_Candidate = new Candidate(m_Person.SearchFirmId, m_Assignment.Id, m_Person.Id)
            {
                InterviewProgressState = new InterviewProgress
                {
                    Stage = CandidateStageEnum.FirstClientInterview,
                    Status = CandidateStatusEnum.Rejected
                }
            };


            m_Candidate = await m_ClassFixture.Server.InsertItemIntoCosmos(TestDataManipulator.CandidateContainerName,
                                                                           m_Candidate.SearchFirmId, m_Candidate);
        }
    }
}
