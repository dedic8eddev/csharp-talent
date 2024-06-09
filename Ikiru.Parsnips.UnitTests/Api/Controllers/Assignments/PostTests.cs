using Ikiru.Parsnips.Api.Controllers.Assignments;
using Ikiru.Parsnips.Application.Services.Assignment;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.UnitTests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Post = Ikiru.Parsnips.Api.Controllers.Assignments.Post;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Assignments
{
    public class PostTests
    {
        private readonly FakeCosmos m_FakeCosmos;
        private readonly Mock<IAssignmentService> _assignmentServiceMock;

        private readonly Post.Command m_Command = new Post.Command
        {
            Name = "Ref 0x311a7ed3 - Wayne Technologies - Investigator",
            CompanyName = "Wayne Enterprises",
            JobTitle = "Investigator",
            Location = "Gotham City",
            StartDate = DateTimeOffset.Now.AddDays(-7).ToOffset(new TimeSpan(-8, 0, 0))
        };

        private readonly Guid m_SearchFirmId = Guid.NewGuid();

        public PostTests()
        {

            _assignmentServiceMock = new Mock<IAssignmentService>();
            m_FakeCosmos = new FakeCosmos()
               .EnableContainerInsert<Assignment>(FakeCosmos.AssignmentsContainerName);
        }

        [Fact]
        public async Task PostCreatesAssignment()
        {
            // Given
            var controller = CreateController();

            // When  
            var actionResult = await controller.Post(m_Command);

            // Then
            var container = m_FakeCosmos.AssignmentsContainer;
            var result = (Post.Result)((CreatedAtActionResult)actionResult).Value;
            container.Verify(c => c.CreateItemAsync(
                It.Is<Assignment>(a 
                                      => a.Id == result.Id
                                         && a.SearchFirmId == m_SearchFirmId
                                         && a.Name == m_Command.Name
                                         && a.CompanyName == m_Command.CompanyName
                                         && a.JobTitle == m_Command.JobTitle
                                         && a.Location == m_Command.Location
                                         && a.StartDate == m_Command.StartDate.Value
                                         && a.Status == AssignmentStatus.Active
                                         ), 
                It.Is<PartitionKey?>(p => p.Equals(new PartitionKey(m_SearchFirmId.ToString()))), 
                It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task PostReturnsCreatedAssignment()
        {
            // Given
            var controller = CreateController();

            // When  
            var actionResult = await controller.Post(m_Command);

            // Then
            var result = (Post.Result)((CreatedAtActionResult)actionResult).Value;
            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Equal(m_Command.Name, result.Name);
            Assert.Equal(m_Command.CompanyName, result.CompanyName);
            Assert.Equal(m_Command.JobTitle, result.JobTitle);
            Assert.Equal(m_Command.Location, result.Location);
            // ReSharper disable once PossibleInvalidOperationException
            Assert.Equal(m_Command.StartDate.Value, result.StartDate);
            Assert.Equal(AssignmentStatus.Active, result.Status);
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
