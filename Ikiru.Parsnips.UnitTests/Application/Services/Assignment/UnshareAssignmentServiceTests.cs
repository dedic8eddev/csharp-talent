using Ikiru.Parsnips.Application.Services.Assignment;
using Ikiru.Parsnips.Application.Services.Assignment.Models;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ResourceNotFound;
using Ikiru.Parsnips.UnitTests.Helpers;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Application.Services.Assignment
{
    public class UnshareAssignmentServiceTests
    {
        private readonly string _email = "john@smith.com";
        private readonly string _existingUserName = "existing user name";

        private readonly FakeRepository _fakeRepository = new FakeRepository();
        private readonly Guid _searchFirmId = Guid.NewGuid();
        private readonly Guid _assignmentId;
        private readonly Guid _portalUserId;
        private readonly Domain.PortalUser _portalUser;

        private readonly UnshareAssignmentCommand _command;

        public UnshareAssignmentServiceTests()
        {
            var assignment = new Domain.Assignment(_searchFirmId);
            _assignmentId = assignment.Id;
            
            _portalUser = new Domain.PortalUser(_searchFirmId)
            {
                Email = _email,
                UserName = _existingUserName
            };
            _portalUserId = _portalUser.Id;

            _fakeRepository.AddToRepository(assignment, _portalUser);

            _command = new UnshareAssignmentCommand
            {
                SearchFirmId = _searchFirmId,
                AssignmentId = _assignmentId,
                Email = _email
            };
        }

        [Fact]
        public async Task ShareRemovesAssignmentIfShared()
        {
            // Arrange
            _portalUser.SharedAssignments.Add(new PortalSharedAssignment(_assignmentId, Guid.NewGuid()));
            var service = CreateService();

            // Act
            await service.Delete(_command);

            // Assert
            var portalUser = await _fakeRepository.GetItem<Domain.PortalUser>(_searchFirmId.ToString(), _portalUserId.ToString());
            var assignments = portalUser.SharedAssignments.Where(a => a.AssignmentId == _assignmentId).ToList(); 
            Assert.Empty(assignments);
        }

        [Fact]
        public async Task ShareDoesNotRemoveOtherAssignments()
        {
            // Arrange
            _portalUser.SharedAssignments.Add(new PortalSharedAssignment(Guid.NewGuid(), Guid.NewGuid()));
            _portalUser.SharedAssignments.Add(new PortalSharedAssignment(Guid.NewGuid(), Guid.NewGuid()));
            var service = CreateService();

            // Act
            await service.Delete(_command);

            // Assert
            var portalUser = await _fakeRepository.GetItem<Domain.PortalUser>(_searchFirmId.ToString(), _portalUserId.ToString());
            Assert.Equal(2, portalUser.SharedAssignments.Count);
        }

        [Fact]
        public async Task ShareThrowsWhenNoAssignment()
        {
            // Arrange
            _command.AssignmentId = Guid.NewGuid();
            var service = CreateService();

            // Act
            var ex = await Record.ExceptionAsync(() => service.Delete(_command));

            // Assert
            Assert.NotNull(ex);
            Assert.IsType<ResourceNotFoundException>(ex);
        }

        private AssignmentService CreateService()
        {
            return new ServiceBuilder<AssignmentService>()
                  .SetFakeRepository(_fakeRepository)
                  .Build();
        }
    }
}