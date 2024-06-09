using Ikiru.Parsnips.Application.Services;
using Ikiru.Parsnips.Application.Services.Assignment;
using Ikiru.Parsnips.Application.Services.Assignment.Models;
using Ikiru.Parsnips.UnitTests.Helpers;
using Moq;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Application.Services.Assignment
{
    public class GetSimpleAssignmentServiceTests
    {
        private readonly FakeRepository _fakeRepository = new FakeRepository();
        private readonly Guid _searchFirmId = Guid.NewGuid();
        private readonly Guid _noActiveAssignmentsSearchFirmId = Guid.NewGuid();
        private readonly int _totalItemCount = 2;
        private readonly Domain.Assignment _assignment2;
        private readonly Domain.Assignment _assignment3;
        private readonly Mock<INoteService> _noteServiceMock;

        public GetSimpleAssignmentServiceTests()
        {
            _noteServiceMock = new Mock<INoteService>();

            var assignment1 = new Domain.Assignment(_searchFirmId) { Name = "assignment 1", CompanyName = "company 1", JobTitle = "job title 1", Location = "location 1", Status = Domain.Enums.AssignmentStatus.Active };
            _assignment2 = new Domain.Assignment(_searchFirmId) { Name = "assignment 2", CompanyName = "company 2", JobTitle = "job title 2", Location = "location 2", Status = Domain.Enums.AssignmentStatus.Active };
            _assignment3 = new Domain.Assignment(_searchFirmId) { Name = "assignment 3", CompanyName = "company 3", JobTitle = "job title 3", Location = "location 3", Status = Domain.Enums.AssignmentStatus.Active };

            SetCreatedDate(assignment1, _assignment2.CreatedDate.AddDays(-2));
            SetCreatedDate(_assignment2, _assignment2.CreatedDate.AddDays(-1));

            var inactiveAssignment1 = new Domain.Assignment(_searchFirmId) { Name = "inactive assignment 1", CompanyName = "inactive company 1", JobTitle = "inactive job title 1", Location = "inactive location 1", StartDate = DateTimeOffset.UtcNow.Date.AddDays(30), Status = Domain.Enums.AssignmentStatus.Abandoned };
            var inactiveAssignment2 = new Domain.Assignment(_searchFirmId) { Name = "inactive assignment 2", CompanyName = "inactive company 2", JobTitle = "inactive job title 2", Location = "inactive location 2", StartDate = DateTimeOffset.UtcNow.Date.AddDays(20), Status = Domain.Enums.AssignmentStatus.OnHold };
            var inactiveAssignment3 = new Domain.Assignment(_searchFirmId) { Name = "inactive assignment 3", CompanyName = "inactive company 3", JobTitle = "inactive job title 3", Location = "inactive location 3", StartDate = DateTimeOffset.UtcNow.Date.AddDays(11), Status = Domain.Enums.AssignmentStatus.Placed };

            var anotherSearchFirmAssignment1 = new Domain.Assignment(Guid.NewGuid()) { Name = "assignment 1", CompanyName = "other company 1", JobTitle = "other job title 1", Location = "other location 1", StartDate = DateTimeOffset.UtcNow.Date.AddDays(40), Status = Domain.Enums.AssignmentStatus.Active };
            var anotherSearchFirmAssignment2 = new Domain.Assignment(Guid.NewGuid()) { Name = "assignment 2", CompanyName = "other company 2", JobTitle = "other job title 2", Location = "other location 2", StartDate = DateTimeOffset.UtcNow.Date.AddDays(50), Status = Domain.Enums.AssignmentStatus.Active };

            var inactiveAssignmentSearchFirm2 = new Domain.Assignment(_noActiveAssignmentsSearchFirmId) { Name = "inactive assignment 3", CompanyName = "inactive company 3", JobTitle = "inactive job title 3", Location = "inactive location 3", StartDate = DateTimeOffset.UtcNow.Date.AddDays(11), Status = Domain.Enums.AssignmentStatus.Placed };

            _fakeRepository.AddToRepository(inactiveAssignment1, inactiveAssignment2, inactiveAssignment3, assignment1, _assignment2, _assignment3, anotherSearchFirmAssignment1, anotherSearchFirmAssignment2, inactiveAssignmentSearchFirm2);
        }

        private void SetCreatedDate(Domain.Assignment item, DateTimeOffset createdDate)
        {
            var itemType = item.GetType();
            var baseType = itemType.BaseType.BaseType.GetTypeInfo();

            var field = baseType.GetDeclaredField("<CreatedDate>k__BackingField");
            field.SetValue(item, createdDate);
        }

        [Fact]
        public async Task GetSimpleListReturnsCorrectResult()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = await service.GetSimple(_searchFirmId, _totalItemCount);

            // Assert
            Assert.Null(result.Value.HasAssignments);

            Assert.Equal(_totalItemCount, result.Value.SimpleActiveAssignments.Count);

            AssertAssignment(_assignment3, result.Value.SimpleActiveAssignments[0]);
            AssertAssignment(_assignment2, result.Value.SimpleActiveAssignments[1]);
        }

        private void AssertAssignment(Domain.Assignment expectedAssignment, SimpleActiveAssignment resultAssignment)
        {
            Assert.Equal(expectedAssignment.Id, resultAssignment.Id);
            Assert.Equal(expectedAssignment.Name, resultAssignment.Name);
            Assert.Equal(expectedAssignment.CompanyName, resultAssignment.CompanyName);
            Assert.Equal(expectedAssignment.Location, resultAssignment.Location);
            Assert.Equal(expectedAssignment.JobTitle, resultAssignment.JobTitle);
            Assert.Equal(expectedAssignment.StartDate, resultAssignment.StartDate);
        }

        [Theory, CombinatorialData]
        public async Task GetSimpleListReturnsCorrectHasAssignments(bool inactiveAssignmentPresent)
        {
            // Arrange
            var searchFirmId = inactiveAssignmentPresent ? _noActiveAssignmentsSearchFirmId : Guid.NewGuid();
            var service = CreateService();

            // Act
            var result = await service.GetSimple(searchFirmId, _totalItemCount);

            // Assert
            Assert.Equal(inactiveAssignmentPresent, result.Value.HasAssignments);
        }

        private AssignmentService CreateService()
        {
            return new ServiceBuilder<AssignmentService>()
                  .SetFakeRepository(_fakeRepository)
                  .AddTransient(_noteServiceMock.Object)
                  .Build();
        }
    }
}
