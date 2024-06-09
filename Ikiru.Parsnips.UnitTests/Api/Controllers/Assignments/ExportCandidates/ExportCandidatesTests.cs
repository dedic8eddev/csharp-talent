using Ikiru.Parsnips.Api.Controllers.Assignments.ExportCandidates;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ResourceNotFound;
using Ikiru.Parsnips.UnitTests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Assignments.ExportCandidates
{
    public class ExportCandidatesTests
    {
        private readonly Guid m_AssignmentId;
        private readonly Guid m_AssignmentWithoutCandidatesId;

        private readonly Guid m_SearchFirmId = Guid.NewGuid();

        private readonly Person m_Person1;
        private readonly Person m_Person2;
        private readonly Candidate m_Candidate1;
        private readonly Candidate m_Candidate2;

        private readonly FakeCosmos m_FakeCosmos = new FakeCosmos();

        private readonly Mock<IDataPoolApi> m_DataPoolApi;
        private readonly Assignment m_Assignment;
        private Shared.Infrastructure.DataPoolApi.Models.Person.Person m_DataPoolPerson;

        public ExportCandidatesTests()
        {
            m_Assignment = new Assignment(m_SearchFirmId);
            var assignmentWithoutCandidates = new Assignment(m_SearchFirmId);

            m_AssignmentId = m_Assignment.Id;
            m_AssignmentWithoutCandidatesId = assignmentWithoutCandidates.Id;

            var otherAssignment = new Assignment(m_SearchFirmId);
            m_FakeCosmos
               .EnableContainerLinqQuery<Assignment, string>(FakeCosmos.AssignmentsContainerName, m_SearchFirmId.ToString(), () => new[] { m_Assignment, assignmentWithoutCandidates, otherAssignment });


            m_Person1 = new Person(m_SearchFirmId, linkedInProfileUrl: "https://uk.linkedin.com/pub/profile-1") { Name = "First profile", Location = "Basingstoke", Organisation = "First organisation", JobTitle = "CEO", TaggedEmails = new List<TaggedEmail> { new TaggedEmail { Email = "a@b.com" }, new TaggedEmail { Email = "c@d.com" } }, PhoneNumbers = new List<string> { "01256333222", "0201234567" } };
            m_Person2 = new Person(m_SearchFirmId, linkedInProfileUrl: "https://uk.linkedin.com/pub/profile-2") { Name = "Second profile", Location = "London", Organisation = "Second organisation", JobTitle = "CTO", TaggedEmails = new List<TaggedEmail> { new TaggedEmail { Email = "second@test.com" } }, PhoneNumbers = new List<string> { "020333222111" } };
            var person3 = new Person(m_SearchFirmId, linkedInProfileUrl: "https://uk.linkedin.com/pub/profile-3");
            var person4 = new Person(m_SearchFirmId, linkedInProfileUrl: "https://uk.linkedin.com/pub/profile-4");
            var person5 = new Person(m_SearchFirmId, linkedInProfileUrl: "https://uk.linkedin.com/pub/profile-5");

            m_FakeCosmos
               .EnableContainerLinqQuery(FakeCosmos.PersonsContainerName,
                                         m_SearchFirmId.ToString(), () => new[] { m_Person1, person4, m_Person2, person3, person5 });

            m_Candidate1 = new Candidate(m_SearchFirmId, m_Assignment.Id, m_Person1.Id) { InterviewProgressState = new InterviewProgress { Status = CandidateStatusEnum.LeftMessage, Stage = CandidateStageEnum.Placed } };
            m_Candidate2 = new Candidate(m_SearchFirmId, m_Assignment.Id, m_Person2.Id) { InterviewProgressState = new InterviewProgress { Status = CandidateStatusEnum.Rejected, Stage = CandidateStageEnum.FirstClientInterview } };
            var candidate3 = new Candidate(m_SearchFirmId, otherAssignment.Id, person3.Id) { InterviewProgressState = new InterviewProgress { Status = CandidateStatusEnum.Withdrew, Stage = CandidateStageEnum.InternalInterview } };
            var candidate4 = new Candidate(m_SearchFirmId, otherAssignment.Id, person4.Id) { InterviewProgressState = new InterviewProgress { Status = CandidateStatusEnum.Rejected, Stage = CandidateStageEnum.ShortList } };
            m_FakeCosmos
               .EnableContainerLinqQuery(FakeCosmos.CandidatesContainerName,
            m_SearchFirmId.ToString(), () => new[] { m_Candidate1, candidate3, m_Candidate2, candidate4 });

            m_DataPoolPerson =
                new Shared.Infrastructure.DataPoolApi.Models.Person.Person
                {
                    Id = Guid.NewGuid(),
                    PersonDetails = new Shared.Infrastructure.DataPoolApi.Models.Person.PersonDetails
                    {
                        Name = "Joe Blogs"
                    },
                    Location = new Shared.Infrastructure.DataPoolApi.Models.Common.Address
                    {
                        MunicipalitySubdivision = "Address",
                        Municipality = "City",
                        Country = "United Kingdom"
                    },
                    CurrentEmployment = new Shared.Infrastructure.DataPoolApi.Models.Person.Job
                    {
                        Position = "Head of World Affairs",
                        CompanyName = "THe Really Big Company"
                    },
                    WebsiteLinks = new List<Shared.Infrastructure.DataPoolApi.Models.Common.WebLink>
                    {
                        new Shared.Infrastructure.DataPoolApi.Models.Common.WebLink
                        {
                            Id = Guid.NewGuid(),
                            LinkTo = Shared.Infrastructure.DataPoolApi.Models.Common.Linkage.LinkedInProfile,
                            Url = "https://www.linkedin.com/in/leonardn"
                        }
                    }
                };

            m_DataPoolApi = new Mock<IDataPoolApi>();
            m_DataPoolApi
                .Setup(x => x.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(m_DataPoolPerson);
        }

        [Theory]
        [InlineData("", "Assignment")]
        [InlineData(" ", "Assignment")]
        [InlineData(null, "Assignment")]
        [InlineData("My wonderful assignment", "My wonderful assignment")]
        public async Task GetReturnsCorrectFileName(string name, string expectedName)
        {
            // Given
            m_Assignment.Name = name;
            var controller = CreateController();

            // When
            var actionResult = await controller.Get(m_AssignmentId);

            // Then
            var fileContent = (FileContentResult)actionResult;

            expectedName = $"{expectedName}-{DateTimeOffset.UtcNow:yyyy-MM-dd}.csv";
            Assert.Equal(expectedName, fileContent.FileDownloadName);
        }

        [Fact]
        public async Task GetReturnsCorrectDocument()
        {
            // Given
            var controller = CreateController();

            // When
            var actionResult = await controller.Get(m_AssignmentId);

            // Then
            var fileContent = (FileContentResult)actionResult;

            AssertExportData(fileContent.FileContents);
        }

        [Fact]
        public async Task GetReturnsEmptyResultWhenNoCandidates()
        {
            // Given
            var controller = CreateController();

            // When
            var actionResult = await controller.Get(m_AssignmentWithoutCandidatesId);

            // Then
            var fileContent = (FileContentResult)actionResult;

            AssertEmptyData(fileContent.FileContents);
        }

        [Theory]
        [InlineData("Arthur C. Clarke", "Arthur", "C. Clarke")]
        [InlineData("Mowgli", "Mowgli", "")]
        [InlineData("Egon Spengler", "Egon", "Spengler")]
        [InlineData(" Dana Barrett", "Dana", "Barrett")]
        [InlineData("Cinderella ", "Cinderella", "")]
        [InlineData(null, "", "")]
        [InlineData("", "", "")]
        [InlineData("  ", "", "")]
        public async Task GetReturnsCorrectFirstAndLastName(string fullName, string firstsName, string lastName)
        {
            // Given
            m_Person1.Name = fullName;
            var controller = CreateController();

            // When
            var actionResult = await controller.Get(m_AssignmentId);

            // Then
            var fileContent = (FileContentResult)actionResult;

            AssertNames(fileContent.FileContents, firstsName, lastName);
        }

        [Theory]
        [InlineData("69 Joy Lane", "Dream Land", "Perfectville", "\"69 Joy Lane, Perfectville, Dream Land\"")]
        [InlineData("1428 Elm Street", "", "Spingwood", "\"1428 Elm Street, Spingwood\"")]
        [InlineData("   ", null, "Reading", "Reading")]
        [InlineData("Eastleigh", null, null, "Eastleigh")]
        public async Task GetReturnsCorrectAddress(string addressLine, string country, string city, string expectedLocation)
        {
            // Given
            m_DataPoolPerson.Location.CountrySubdivisionName = city;
            m_DataPoolPerson.Location.Country = country;
            m_DataPoolPerson.Location.Municipality = addressLine; 

            m_Person1.Location = "";
            m_Person1.DataPoolPersonId = Guid.NewGuid();

            var controller = CreateController();

            // When
            var actionResult = await controller.Get(m_AssignmentId);

            // Then
            var fileContent = (FileContentResult)actionResult;

            var response = GetResponseArray(fileContent.FileContents);
            var firstPersonExport = response[1];
            Assert.Contains($",{expectedLocation},", firstPersonExport);
        }

        [Fact]
        public async Task GetThrowsWhenNoAssignment()
        {
            // Given
            var controller = CreateController();

            // Then
            var ex = await Record.ExceptionAsync(() => controller.Get(Guid.NewGuid()));

            // Then
            Assert.IsType<ResourceNotFoundException>(ex);
        }

        private static void AssertNames(byte[] exportData, string firstsName, string lastName)
        {
            var response = GetResponseArray(exportData);

            var firstPersonExport = response[1];

            Assert.Contains($",{firstsName},", firstPersonExport);
            Assert.Contains($",{lastName},", firstPersonExport);
        }

        private void AssertExportData(byte[] exportData)
        {
            var response = GetResponseArray(exportData);

            Assert.True(response.Length >= 3);
            ValidateHeader(response);

            AssertExportLine(response, m_Person1, m_Candidate1);
            AssertExportLine(response, m_Person2, m_Candidate2);
        }

        private static void AssertEmptyData(byte[] exportData)
        {
            var response = GetResponseArray(exportData);

            Assert.True(response.Length == 1);
            ValidateHeader(response);
        }

        private static string[] GetResponseArray(byte[] exportData)
        {
            var export = Encoding.UTF8.GetString(exportData);

            return export.Split('\n').Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
        }

        private static void ValidateHeader(string[] response)
        {
            var header = response[0];

            Assert.StartsWith("Name", header);
            Assert.Contains("FirstName", header);
            Assert.Contains("LastName", header);
            Assert.Contains("Location", header);
            Assert.Contains("JobTitle", header);
            Assert.Contains("Organisation", header);
            Assert.Contains("EmailAddresses", header);
            Assert.Contains("PhoneNumbers", header);
            Assert.Contains("LinkedInProfileUrl", header);
            Assert.Contains("Stage", header);
            Assert.Contains("Status", header);
        }

        private static void AssertExportLine(string[] response, Person person, Candidate candidate)
        {
            var personExport = response.Single(l => l.Contains(person.Name));

            Assert.Contains(person.Location, personExport);
            Assert.Contains(person.Location, personExport);
            Assert.Contains(person.JobTitle, personExport);
            Assert.Contains(person.Organisation, personExport);
            person.TaggedEmails.ForEach(e => Assert.Contains(e.Email, personExport));
            person.PhoneNumbers.ForEach(e => Assert.Contains(e, personExport));
            Assert.Contains(person.LinkedInProfileId, personExport);
            Assert.Contains(candidate.InterviewProgressState.Status.ToString(), personExport);
            Assert.Contains(candidate.InterviewProgressState.Stage.ToString(), personExport);
        }

        private ExportCandidatesController CreateController()
        {
            return new ControllerBuilder<ExportCandidatesController>()
                  .SetFakeCosmos(m_FakeCosmos)
                  .SetSearchFirmUser(m_SearchFirmId)
                  .AddTransient(m_DataPoolApi.Object)
                  .Build();
        }
    }
}
