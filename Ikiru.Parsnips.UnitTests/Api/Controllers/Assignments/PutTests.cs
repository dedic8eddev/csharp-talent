using Ikiru.Parsnips.Api.Controllers.Assignments;
using Ikiru.Parsnips.Application.Services.Assignment;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ResourceNotFound;
using Ikiru.Parsnips.UnitTests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Moq;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Assignments
{
    public class PutTests
    {
        private readonly Guid m_MissingAssignmentId = Guid.NewGuid();
        private readonly Assignment m_Assignment;
        private readonly Put.Command m_Command;
        private readonly FakeCosmos m_FakeCosmos;
        private readonly Guid m_SearchFirmId = Guid.NewGuid();
        private readonly Mock<IAssignmentService> _assignmentServiceMock;

        public PutTests()
        {
            _assignmentServiceMock = new Mock<IAssignmentService>();
            m_Assignment = new Assignment(m_SearchFirmId)
            {
                CompanyName = "CompanyName",
                JobTitle = "JobTitle",
                Location = "Location",
                Name = "Location",
                StartDate = DateTime.Now.AddDays(1),
                Status = AssignmentStatus.Abandoned
            };

            m_Command = new Put.Command
            {
                CompanyName = "Updated CompanyName",
                JobTitle = "Updated JobTitle",
                Location = "Update Location",
                Name = "Update Name",
                StartDate = DateTime.Now.AddDays(2),
                Status = AssignmentStatus.OnHold
            };

            m_FakeCosmos = new FakeCosmos()
                .EnableContainerFetch(FakeCosmos.AssignmentsContainerName, m_Assignment.Id.ToString(),
                                                    m_SearchFirmId.ToString(), () => m_Assignment)
                .EnableContainerReplace<Assignment>(FakeCosmos.AssignmentsContainerName, m_Assignment.Id.ToString(),
                                                    m_SearchFirmId.ToString())
                .EnableContainerFetchThrowCosmosException<Assignment>(FakeCosmos.AssignmentsContainerName, m_MissingAssignmentId.ToString(),
                                                                    m_SearchFirmId.ToString(), HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task PutUpdateAssignment()
        {
            // Given
            var controller = CreateController();
            var container = m_FakeCosmos.AssignmentsContainer;

            // When
            var actionResult = await controller.Put(m_Assignment.Id, m_Command);

            // Then
            Assert.IsType<OkObjectResult>(actionResult);
            container.Verify(c => c.ReplaceItemAsync(It.Is<Assignment>(a => a.JobTitle == m_Command.JobTitle &&
                                                                            a.Location == m_Command.Location &&
                                                                            a.Name == m_Command.Name &&
                                                                            a.CompanyName == m_Command.CompanyName &&
                                                                            a.SearchFirmId == m_SearchFirmId &&
                                                                            a.StartDate == m_Command.StartDate &&
                                                                            a.Status == m_Command.Status),
                                                                            It.Is<string>(x => x == m_Assignment.Id.ToString()),
                                                                            It.Is<PartitionKey>(x => x == new PartitionKey(m_SearchFirmId.ToString())),
                                                                            It.IsAny<ItemRequestOptions>(),
                                                                            It.IsAny<CancellationToken>()));

        }

        [Fact]
        public async Task PutReturnsUpdatedAssignment()
        {
            // Given
            var controller = CreateController();

            // When
            var actionResult = await controller.Put(m_Assignment.Id, m_Command);

            // Then
            var result = (Put.Result)((OkObjectResult)actionResult).Value;

            Assert.Equal(m_Command.Name, result.Name);
            Assert.Equal(m_Command.Location, result.Location);
            Assert.Equal(m_Command.JobTitle, result.JobTitle);
            Assert.Equal(m_Command.StartDate, result.StartDate);
            Assert.Equal(m_Command.CompanyName, result.CompanyName);
            Assert.Equal(m_Command.Status, result.Status);
            Assert.Equal(m_Assignment.Id, result.Id);

        }
             

        [Fact]
        public async Task PutThrowWhenAssignmentNotExist()
        {
            // Given
            var controller = CreateController();

            // When
            var exception = await Record.ExceptionAsync(() => controller.Put(m_MissingAssignmentId, m_Command));

            // Then
            Assert.IsType<ResourceNotFoundException>(exception);
        }

        private AssignmentsController CreateController()
        {
            return new ControllerBuilder<AssignmentsController>()
                .SetFakeCosmos(m_FakeCosmos)
                .SetSearchFirmUser(m_SearchFirmId)

                  .AddTransient(_assignmentServiceMock.Object)
                .Build();
        }
    }
}
