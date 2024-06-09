using Ikiru.Parsnips.Api.Controllers.Assignments;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using Ikiru.Parsnips.Application.Services.Assignment.Models;
using Ikiru.Parsnips.Application.Services.Assignment;
using Ikiru.Parsnips.Application.Services;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Assignments
{
    public class GetSimpleListTests
    {
        private readonly Mock<IAssignmentService> _assignmentServiceMock = new Mock<IAssignmentService>();
        private readonly Guid _searchFirmId = Guid.NewGuid();
        private readonly int _totalItemCount = 3;
        private readonly ServiceResponse<ActiveAssignmentSimpleResponse> _activeAssignmentSimpleResponse;

        public GetSimpleListTests()
        {
            _activeAssignmentSimpleResponse = new ServiceResponse<ActiveAssignmentSimpleResponse>()
            {
                Value = new ActiveAssignmentSimpleResponse()
                {
                    SimpleActiveAssignments = new List<SimpleActiveAssignment>
                    {
                        new SimpleActiveAssignment { Id = Guid.NewGuid(), Name = "assignment 1", CompanyName = "company 1", Location = "location 1", StartDate = DateTimeOffset.UtcNow.Date.AddDays(10), JobTitle = "job title 1"},
                        new SimpleActiveAssignment { Id = Guid.NewGuid(), Name = "assignment 2", CompanyName = "company 2", Location = "location 2", StartDate = DateTimeOffset.UtcNow.Date.AddDays(20), JobTitle = "job title 2"}
                    }
                }
            };
            _assignmentServiceMock
               .Setup(s => s.GetSimple(It.Is<Guid>(id => id == _searchFirmId), It.Is<int>(c => c == _totalItemCount)))
               .ReturnsAsync(_activeAssignmentSimpleResponse);
        }

        [Fact]
        public async Task GetSimpleListReturnsCorrectResult()
        {
            // Arrange
            var controller = CreateController();

            // Act
            var actionResult = await controller.GetSimpleList(_totalItemCount);

            // Assert
            var result = (ActiveAssignmentSimpleResponse)((OkObjectResult)actionResult).Value;
            Assert.Equal(_activeAssignmentSimpleResponse.Value.SimpleActiveAssignments, result.SimpleActiveAssignments);
            Assert.Null(result.HasAssignments);
        }

        private AssignmentsController CreateController()
        {
            return new ControllerBuilder<AssignmentsController>()
                  .AddTransient(_assignmentServiceMock.Object)
                  .SetSearchFirmUser(_searchFirmId)
                  .Build();
        }
    }
}
