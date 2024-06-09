using Ikiru.Parsnips.Api.Controllers.Candidates;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.IntegrationTests.Helpers;
using Ikiru.Parsnips.IntegrationTests.Helpers.Data;
using Ikiru.Parsnips.IntegrationTests.Helpers.External;
using Ikiru.Parsnips.IntegrationTests.Helpers.HttpMessages;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Ikiru.Parsnips.IntegrationTests.Features.Api.Candidates
{
    [Collection(nameof(IntegrationTestCollection))]
    public class CandidatesTests : IntegrationTestBase, IClassFixture<CandidatesTests.CandidatesTestsClassFixture>
    {
        private readonly CandidatesTestsClassFixture m_ClassFixture;
        private static readonly Guid m_DataPoolPersonId = Guid.NewGuid();

        public CandidatesTests(IntegrationTestFixture fixture, CandidatesTestsClassFixture classFixture, ITestOutputHelper output) : base(fixture, output)
        {
            m_ClassFixture = classFixture;
        }

        public sealed class CandidatesTestsClassFixture : IDisposable
        {
            public IntTestServer Server { get; }

            // ReSharper disable once UnusedParameter.Local
            public CandidatesTestsClassFixture()
            {
                Server = new TestServerBuilder()
                    .AddSingleton(FakeDatapoolApi.Setup(CandidatesTests.m_DataPoolPersonId).Object)
                   .Build();
            }

            public void Dispose()
            {
                Server.Dispose();
            }
        }

        private Assignment m_Assignment;
        private Person m_Person;
        private Candidate m_Candidate;

        private async Task EnsureDependentItemsInCosmos()
        {
            m_Assignment = new Assignment(m_ClassFixture.Server.Authentication.DefaultSearchFirmId)
            {
                Name = "Assignment for Candidate Tests v2",
                CompanyName = "A Company",
                JobTitle = "A Job Title"
            };

            m_Assignment = await m_ClassFixture.Server.AddUniqueItemIntoCosmos(TestDataManipulator.AssignmentsContainerName, m_Assignment.SearchFirmId, a => a.Name == m_Assignment.Name, m_Assignment);

            m_Person = new Person(m_ClassFixture.Server.Authentication.DefaultSearchFirmId)
            {
                Name = $"Person for Candidate Tests v-{Guid.NewGuid()}",
                JobTitle = "A Job Title",
                Organisation = "A Company",
                DataPoolPersonId = m_DataPoolPersonId
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

        [Fact]
        public async Task PostShouldRespondWithOkResult()
        {
            // Given
            await EnsureDependentItemsInCosmos();

            var newPerson = new Person(m_ClassFixture.Server.Authentication.DefaultSearchFirmId)
            {
                Name = "New Person for Candidate Tests Post",
                JobTitle = "A Job Title",
                Organisation = "A Company"
            };

            var defaultInterviewProgresStage = new InterviewProgress { Stage = CandidateStageEnum.Identified, Status = CandidateStatusEnum.NoStatus };

            newPerson = await m_ClassFixture.Server.InsertItemIntoCosmos(TestDataManipulator.PersonsContainerName, m_Assignment.SearchFirmId, newPerson);

            var command = new
            {
                AssignmentId = m_Assignment.Id,
                PersonId = newPerson.Id
            };

            // When
            var response = await m_ClassFixture.Server.Client.PostAsync("/api/candidates", new JsonContent(command));

            // Then
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var r = new
            {
                Id = Guid.Empty,
                AssignmentId = Guid.Empty,
                PersonId = Guid.Empty,
                LinkAssignment = new
                {
                    Id = Guid.Empty,
                    Name = "",
                    CompanyName = "",
                    JobTitle = ""
                },
                LinkPerson = new
                {
                    Id = Guid.Empty,
                    Name = "",
                    Company = "",
                    JobTitle = ""
                },
                InterviewProgressState = new InterviewProgress()
            };

            var responseJson = await response.Content.DeserializeToAnonymousType(r);

            Assert.NotEqual(Guid.Empty, responseJson.Id);
            Assert.Equal(m_Assignment.Id, responseJson.AssignmentId);
            Assert.Equal(newPerson.Id, responseJson.PersonId);

            Assert.NotNull(responseJson.LinkAssignment);
            Assert.Equal(m_Assignment.Id, responseJson.LinkAssignment.Id);
            Assert.Equal(m_Assignment.Name, responseJson.LinkAssignment.Name);
            Assert.Equal(m_Assignment.CompanyName, responseJson.LinkAssignment.CompanyName);
            Assert.Equal(m_Assignment.JobTitle, responseJson.LinkAssignment.JobTitle);

            Assert.NotNull(responseJson.LinkPerson);
            Assert.Equal(newPerson.Id, responseJson.LinkPerson.Id);
            Assert.Equal(newPerson.Name, responseJson.LinkPerson.Name);
            Assert.Equal(newPerson.Organisation, responseJson.LinkPerson.Company);
            Assert.Equal(newPerson.JobTitle, responseJson.LinkPerson.JobTitle);

            Assert.Equal(defaultInterviewProgresStage.Stage, responseJson.InterviewProgressState.Stage);
            Assert.Equal(defaultInterviewProgresStage.Status, responseJson.InterviewProgressState.Status);
        }

        [Fact]
        public async Task GetListShouldRespondWithResults()
        {
            // Given
            await EnsureDependentItemsInCosmos();

            // When
            var response = await m_ClassFixture.Server.Client.GetAsync($"/api/candidates?personId={m_Person.Id}&assignmentId={m_Assignment.Id}&expand=assignment,person&limit=1");

            // Then
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var r = new
            {
                HasMoreResults = false,
                Candidates = new[]
                                     {
                                         new
                                         {
                                             Id = Guid.Empty,
                                             AssignmentId = Guid.Empty,
                                             PersonId = Guid.Empty,
                                             InterviewProgressState = new
                                                                       {
                                                                           Stage = "",
                                                                           Status = ""
                                                                       },
                                             LinkAssignment = new
                                                              {
                                                                  Id = Guid.Empty,
                                                                  Name = "",
                                                                  CompanyName = "",
                                                                  JobTitle = ""
                                                              },
                                             LinkPerson = new
                                                {
                                                 LocalPerson = new
                                                          {
                                                              Id = Guid.Empty,
                                                              DataPoolId = Guid.Empty,
                                                              Name = "",
                                                              JobTitle = "",
                                                              Company = ""
                                                          },
                                                DataPoolPerson = new
                                                          {
                                                              Id = Guid.Empty,
                                                              DataPoolId = Guid.Empty,
                                                              Name = "",
                                                              JobTitle = "",
                                                              Company = ""
                                                          }
                                                 }
                                         }
                                     }
            };
            var responseJson = await response.Content.DeserializeToAnonymousType(r);
            Assert.NotNull(responseJson);
            Assert.NotNull(responseJson.Candidates);
            Assert.False(responseJson.HasMoreResults);
            var candidateResult = Assert.Single(responseJson.Candidates);

            Assert.Equal(m_Assignment.Id, candidateResult.AssignmentId);
            Assert.Equal(m_Person.Id, candidateResult.PersonId);

            Assert.Equal(m_Candidate.InterviewProgressState.Stage.ToString().ToLower(), candidateResult.InterviewProgressState.Stage.ToLower());
            Assert.Equal(m_Candidate.InterviewProgressState.Status.ToString().ToLower(), candidateResult.InterviewProgressState.Status.ToLower());

            Assert.NotNull(candidateResult.LinkAssignment);
            Assert.Equal(m_Assignment.Id, candidateResult.LinkAssignment.Id);
            Assert.Equal(m_Assignment.Name, candidateResult.LinkAssignment.Name);
            Assert.Equal(m_Assignment.CompanyName, candidateResult.LinkAssignment.CompanyName);
            Assert.Equal(m_Assignment.JobTitle, candidateResult.LinkAssignment.JobTitle);

            Assert.NotNull(candidateResult.LinkPerson);
            Assert.Equal(m_Person.Id, candidateResult.LinkPerson.LocalPerson.Id);
            Assert.Equal(m_Person.Name, candidateResult.LinkPerson.LocalPerson.Name);
            Assert.Equal(m_Person.JobTitle, candidateResult.LinkPerson.LocalPerson.JobTitle);
            Assert.Equal(m_Person.Organisation, candidateResult.LinkPerson.LocalPerson.Company);

            Assert.Equal(FakeDatapoolApi.StubData[0].Id, candidateResult.LinkPerson.DataPoolPerson.DataPoolId);
            Assert.Equal(FakeDatapoolApi.StubData[0].PersonDetails.Name, candidateResult.LinkPerson.DataPoolPerson.Name);
            Assert.Equal(FakeDatapoolApi.StubData[0].CurrentEmployment.Position, candidateResult.LinkPerson.DataPoolPerson.JobTitle);
            Assert.Equal(FakeDatapoolApi.StubData[0].CurrentEmployment.CompanyName, candidateResult.LinkPerson.DataPoolPerson.Company);
        }


        [Fact]
        public async Task PutShouldRespondWithResults()
        {
            // Given
            await EnsureDependentItemsInCosmos();

            var putCommand = new
            {
                AssignTo = Guid.NewGuid(),
                DueDate = DateTimeOffset.Now.AddHours(5),
                CandidateId = m_Candidate.Id,
                InterviewProgressState = new Put.Command.InterviewProgress
                {
                    Stage = CandidateStageEnum.FirstClientInterview,
                    Status = CandidateStatusEnum.Withdrew
                }
            };

            // When
            var putResponse = await m_ClassFixture.Server.Client.PutAsync($"/api/candidates/{m_Candidate.Id}", new JsonContent(putCommand));


            // Then
            Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

            var r = new
            {
                PersonId = Guid.Empty,
                AssignmentId = Guid.Empty,
                CandidateId = Guid.Empty,
                InterviewProgressState = new
                {
                    Stage = "",
                    Status = ""
                },
                DueDate = DateTimeOffset.Now.AddMonths(1),
                AssignTo = Guid.NewGuid()
            };

            var responseJson = await putResponse.Content.DeserializeToAnonymousType(r);
            Assert.NotNull(responseJson);

            Assert.Equal(m_Person.Id, responseJson.PersonId);
            Assert.Equal(putCommand.InterviewProgressState.Stage.ToString().ToLower(), responseJson.InterviewProgressState.Stage.ToLower());
            Assert.Equal(putCommand.InterviewProgressState.Status.ToString().ToLower(), responseJson.InterviewProgressState.Status.ToLower());
            Assert.Equal(putCommand.AssignTo, responseJson.AssignTo);
            Assert.Equal(putCommand.DueDate, responseJson.DueDate);
        }

        [Fact]
        public async Task PatchShouldRespondWithResults()
        {
            // Given
            await EnsureDependentItemsInCosmos();

            var command = new
            {
                DueDate = DateTimeOffset.Now.AddHours(5),
            };

            using var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"/api/candidates/{m_Candidate.Id}");

            request.Headers.TryAddWithoutValidation("accept", "*/*");

            request.Content = new JsonContent(command);
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/merge-patch+json");

            // When
            var response = await m_ClassFixture.Server.Client.SendAsync(request);

            // Then
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var r = new
            {
                PersonId = Guid.Empty,
                AssignmentId = Guid.Empty,
                CandidateId = Guid.Empty,
                InterviewProgressState = new
                {
                    Stage = "",
                    Status = ""
                },
                DueDate = DateTimeOffset.Now.AddMonths(1),
                AssignTo = (Guid?)Guid.NewGuid()
            };

            var responseJson = await response.Content.DeserializeToAnonymousType(r);

            Assert.NotNull(responseJson);

            Assert.Equal(m_Person.Id, responseJson.PersonId);
            Assert.Equal(m_Candidate.InterviewProgressState.Stage.ToString().ToLower(), responseJson.InterviewProgressState.Stage.ToLower());
            Assert.Equal(m_Candidate.InterviewProgressState.Status.ToString().ToLower(), responseJson.InterviewProgressState.Status.ToLower());
            Assert.Null(responseJson.AssignTo);
            Assert.Equal(command.DueDate, responseJson.DueDate);
        }
    }
}
