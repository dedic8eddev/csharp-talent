using Ikiru.Parsnips.Api.Controllers.Assignments;
using Ikiru.Parsnips.Application.Services.Assignment;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ResourceNotFound;
using Ikiru.Parsnips.UnitTests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Assignments
{
    public class GetTests
    {
        private readonly FakeCosmos m_FakeCosmos;

        private readonly Assignment m_Assignment;

        private readonly Mock<IAssignmentService> _assignmentServiceMock;
        private readonly Guid m_SearchFirmId = Guid.NewGuid();

        public GetTests()
        {
            _assignmentServiceMock = new Mock<IAssignmentService>();
            m_Assignment = new Assignment(m_SearchFirmId)
            {
                Name = "Ref 0x311a7ed3 - Wayne Technologies - Investigator",
                CompanyName = "Wayne Enterprises",
                JobTitle = "Investigator",
                Location = "Gotham City",
                StartDate = DateTimeOffset.Now.AddDays(-7),
                Status = AssignmentStatus.Placed
            };

            m_FakeCosmos = new FakeCosmos()
               .EnableContainerLinqQuery(FakeCosmos.AssignmentsContainerName,
                                         m_SearchFirmId.ToString(),
                                         () => new List<Assignment> { m_Assignment });
        }

        [Fact]
        public async Task GetReturnsAssignment()
        {
            // Given
            var controller = CreateController();

            // When  
            var actionResult = await controller.Get(m_Assignment.Id);

            // Then
            var result = (Get.Result)((OkObjectResult)actionResult).Value;
            Assert.Equal(m_Assignment.Id, result.Id);
            Assert.Equal(m_Assignment.Name, result.Name);
            Assert.Equal(m_Assignment.CompanyName, result.CompanyName);
            Assert.Equal(m_Assignment.JobTitle, result.JobTitle);
            Assert.Equal(m_Assignment.Location, result.Location);
            Assert.Equal(m_Assignment.StartDate, result.StartDate);
            Assert.Equal(m_Assignment.Status, result.Status);
        }

        [Fact]
        public async Task GetByIdThrowsWhenNoAssignment()
        {
            // Given
            var controller = CreateController();

            // When
            var ex = await Record.ExceptionAsync(() => controller.Get(Guid.NewGuid()));

            // Then
            Assert.NotNull(ex);
            Assert.IsType<ResourceNotFoundException>(ex);
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
