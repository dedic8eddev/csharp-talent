using Ikiru.Parsnips.IntegrationTests.Helpers;
using Ikiru.Parsnips.IntegrationTests.Helpers.Data;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Ikiru.Parsnips.IntegrationTests.Helpers.HttpMessages;

namespace Ikiru.Parsnips.IntegrationTests.Features.Api.Assignments.ExportCandidates
{
    [Collection(nameof(IntegrationTestCollection))]
    public class ExportCandidatesTests : IntegrationTestBase, IClassFixture<ExportCandidatesTests.ExportCandidatesClassFixture>
    {
        private readonly ExportCandidatesClassFixture m_ClassFixture;

        public class ExportCandidatesClassFixture : IDisposable
        {
            public IntTestServer Server { get; }
            public ExportCandidatesClassFixture() => Server = new TestServerBuilder().Build();
            public void Dispose() => Server.Dispose();
        }

        public ExportCandidatesTests(IntegrationTestFixture fixture, ExportCandidatesClassFixture classFixture, ITestOutputHelper output) : base(fixture, output)
        {
            m_ClassFixture = classFixture;
        }

        private Assignment m_Assignment;
        private Person m_Person;
        private Candidate m_Candidate;

        private const string _FIRST_NAME = "George";
        private const string _FAMILY_NAME = "Smith";

        private async Task AddAssignmentIntoCosmos()
        {
            m_Assignment = new Assignment(m_ClassFixture.Server.Authentication.DefaultSearchFirmId)
            {
                Status = AssignmentStatus.Active,
                Name = "Fancy new position",
            };

            m_Person = new Person(m_Assignment.SearchFirmId)
            {
                Name = $"{_FIRST_NAME} {_FAMILY_NAME}",
                Location = "Basingville",
                JobTitle = "CEO",
                Organisation = "Superiors Inc",
                TaggedEmails = new List<TaggedEmail> { new TaggedEmail { Email = $"{_FIRST_NAME}@{_FAMILY_NAME}.co.uk" }, new TaggedEmail { Email = $"{_FAMILY_NAME}@{_FIRST_NAME}.co.uk" } },
                PhoneNumbers = new List<string> { "0123456", "0654321" },
                LinkedInProfileUrl = "https://linkedin.com/in/oldy-georgy"
            };

            m_Assignment = await m_ClassFixture.Server.AddUniqueItemIntoCosmos(
                TestDataManipulator.AssignmentsContainerName,
                m_Assignment.SearchFirmId,
                a => a.Name == m_Assignment.Name,
                m_Assignment);

            m_Person = await m_ClassFixture.Server.AddUniqueItemIntoCosmos(
                TestDataManipulator.PersonsContainerName,
                m_Person.SearchFirmId,
                p => p.Name == m_Person.Name,
                m_Person);

            m_Candidate = new Candidate(m_Assignment.SearchFirmId, m_Assignment.Id, m_Person.Id)
                          {
                              InterviewProgressState = new InterviewProgress
                                                       {
                                                           Status = CandidateStatusEnum.Contacted,
                                                           Stage = CandidateStageEnum.Identified
                                                       }
                          };

            m_Candidate = await m_ClassFixture.Server.AddUniqueItemIntoCosmos(
                TestDataManipulator.CandidateContainerName,
                m_Candidate.SearchFirmId,
                c => c.AssignmentId == m_Assignment.Id && c.PersonId == m_Person.Id,
                m_Candidate);
        }

        [Fact]
        public async Task GetShouldRespondWithExportFile()
        {
            // Given
            await AddAssignmentIntoCosmos();

            // When
            var response = await m_ClassFixture.Server.Client.GetAsync($"/api/assignments/{m_Assignment.Id}/exportcandidates");

            // Then
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.Equal("application/octet-stream", response.Content.Headers.ContentType.MediaType);
            Assert.Equal($"\"{m_Assignment.Name}-{DateTimeOffset.UtcNow:yyyy-MM-dd}.csv\"", response.Content.Headers.ContentDisposition.FileName);

            var responseStream = await response.Content.ReadAsStreamAsync();

            responseStream.Position = 0;
            using var reader = new StreamReader(responseStream, Encoding.UTF8);
            var content = await reader.ReadToEndAsync();

            Assert.Contains($"{m_Person.Name},", content);
            Assert.Contains($",{_FIRST_NAME},", content);
            Assert.Contains($",{_FAMILY_NAME},", content);
            Assert.Contains($",{m_Person.Location},", content);
            Assert.Contains($",{m_Person.JobTitle},", content);
            Assert.Contains($",{m_Person.Organisation},", content);
            Assert.Contains($",{m_Person.TaggedEmails.Select(e => e.Email).First()};", content);
            Assert.Contains($";{m_Person.TaggedEmails.Select(e => e.Email).Last()},", content);
            Assert.Contains($",{m_Person.PhoneNumbers.First()};", content);
            Assert.Contains($";{m_Person.PhoneNumbers.Last()},", content);
            Assert.Contains($",{m_Person.LinkedInProfileUrl},", content);
            Assert.Contains($",{m_Candidate.InterviewProgressState.Stage},", content);
            Assert.Contains($",{m_Candidate.InterviewProgressState.Status}", content);
        }

        [Fact]
        public async Task PostShouldRespondWithExportFile()
        {
            // Given
            await AddAssignmentIntoCosmos();

            var candidateIds = new Guid[] 
            {
               m_Candidate.Id
            };

            // When
            var response = await m_ClassFixture.Server.Client.PostAsync($"/api/assignments/{m_Assignment.Id}/exportcandidates", new JsonContent(candidateIds));

            // Then
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.Equal("application/octet-stream", response.Content.Headers.ContentType.MediaType);
            Assert.Equal($"\"{m_Assignment.Name}-{DateTimeOffset.UtcNow:yyyy-MM-dd}.csv\"", response.Content.Headers.ContentDisposition.FileName);

            var responseStream = await response.Content.ReadAsStreamAsync();

            responseStream.Position = 0;
            using var reader = new StreamReader(responseStream, Encoding.UTF8);
            var content = await reader.ReadToEndAsync();

            Assert.Contains($"{m_Person.Name},", content);
            Assert.Contains($",{_FIRST_NAME},", content);
            Assert.Contains($",{_FAMILY_NAME},", content);
            Assert.Contains($",{m_Person.Location},", content);
            Assert.Contains($",{m_Person.JobTitle},", content);
            Assert.Contains($",{m_Person.Organisation},", content);
            Assert.Contains($",{m_Person.TaggedEmails.Select(e => e.Email).First()};", content);
            Assert.Contains($";{m_Person.TaggedEmails.Select(e => e.Email).Last()},", content);
            Assert.Contains($",{m_Person.PhoneNumbers.First()};", content);
            Assert.Contains($";{m_Person.PhoneNumbers.Last()},", content);
            Assert.Contains($",{m_Person.LinkedInProfileUrl},", content);
            Assert.Contains($",{m_Candidate.InterviewProgressState.Stage},", content);
            Assert.Contains($",{m_Candidate.InterviewProgressState.Status}", content);
        }
    }
}
