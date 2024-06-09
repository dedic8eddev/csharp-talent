using Ikiru.Parsnips.Application.Services.Assignment;
using Ikiru.Parsnips.Application.Services.Assignment.Models;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ResourceNotFound;
using Ikiru.Parsnips.UnitTests.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Application.Services.Assignment
{
    public class GetSharedAssignmentServiceTests
    {
        private readonly FakeRepository _fakeRepository = new FakeRepository();
        private readonly Guid _searchFirmId = Guid.NewGuid();
        private readonly Guid _assignmentId;

        private readonly Domain.PortalUser _portalUser;

        private readonly Domain.PortalUser _anotherPortalUser;

        private readonly GetSharedAssignmentCommand _command;

        public GetSharedAssignmentServiceTests()
        {
            var assignment = new Domain.Assignment(_searchFirmId);
            _assignmentId = assignment.Id;
            
            _portalUser = new Domain.PortalUser(_searchFirmId)
            {
                Email = "john@smith.com",
                UserName = "existing user name",
                SharedAssignments = new List<PortalSharedAssignment>
                {
                    new PortalSharedAssignment(Guid.NewGuid(), Guid.NewGuid()),
                    new PortalSharedAssignment(Guid.NewGuid(), Guid.NewGuid())
                }
            };

            _anotherPortalUser = new Domain.PortalUser(_searchFirmId)
            {
                Email = "another@user.email",
                UserName = "anotherUserName",
                SharedAssignments = new List<PortalSharedAssignment>
                {
                    new PortalSharedAssignment(Guid.NewGuid(), Guid.NewGuid()),
                    new PortalSharedAssignment(Guid.NewGuid(), Guid.NewGuid())
                }
            };

            var unrelatedPortalUser = new Domain.PortalUser(_searchFirmId)
            {
                Email = "unrelated@user.email",
                UserName = "unrelatedUserName",
                SharedAssignments = new List<PortalSharedAssignment>
                {
                    new PortalSharedAssignment(Guid.NewGuid(), Guid.NewGuid()),
                    new PortalSharedAssignment(Guid.NewGuid(), Guid.NewGuid()),
                    new PortalSharedAssignment(Guid.NewGuid(), Guid.NewGuid())
                }
            };

            var unrelatedPortalUser2 = new Domain.PortalUser(Guid.NewGuid())
            {
                Email = "unrelated@user.email",
                UserName = "unrelatedUserName",
                SharedAssignments = new List<PortalSharedAssignment>
                {
                    new PortalSharedAssignment(Guid.NewGuid(), Guid.NewGuid()),
                    new PortalSharedAssignment(Guid.NewGuid(), Guid.NewGuid()),
                    new PortalSharedAssignment(Guid.NewGuid(), Guid.NewGuid()),
                    new PortalSharedAssignment(Guid.NewGuid(), Guid.NewGuid())
                }
            };

            _fakeRepository.AddToRepository(assignment, _portalUser, _anotherPortalUser, unrelatedPortalUser, unrelatedPortalUser2);

            _command = new GetSharedAssignmentCommand
            {
                SearchFirmId = _searchFirmId,
                AssignmentId = _assignmentId,
            };
        }

        [Fact]
        public async Task GetSharedReturnsPortalUsersForAssignmentIfShared()
        {
            // Arrange
            _portalUser.SharedAssignments.Add(new PortalSharedAssignment(_assignmentId, Guid.NewGuid()));
            _anotherPortalUser.SharedAssignments.Add(new PortalSharedAssignment(_assignmentId, Guid.NewGuid()));
            var service = CreateService();

            // Act
            var result = await service.GetShared(_command);

            // Assert
            Assert.NotEmpty(result.PortalUsers);
            Assert.Equal(2, result.PortalUsers.Count);

            var user1 = result.PortalUsers.Single(u => u.Email == _portalUser.Email);
            var user2 = result.PortalUsers.Single(u => u.Email == _anotherPortalUser.Email);

            Assert.Equal(_portalUser.UserName, user1.UserName);
            Assert.Equal(_anotherPortalUser.UserName, user2.UserName);
        }

        [Fact]
        public async Task GetSharedDoesNotReturnPortalUsersForAssignmentIfNotShared()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = await service.GetShared(_command);

            // Assert
            Assert.True(result.PortalUsers == null || !result.PortalUsers.Any(), "PortalUsers is expected to be empty");
        }

        [Fact]
        public async Task GetSharedThrowsWhenNoAssignment()
        {
            // Arrange
            _command.AssignmentId = Guid.NewGuid();
            var service = CreateService();

            // Act
            var ex = await Record.ExceptionAsync(() => service.GetShared(_command));

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