using AutoMapper;
using Ikiru.Parsnips.Application.Services.PortalUser;
using Ikiru.Parsnips.Portal.Api.Controllers;
using Ikiru.Parsnips.UnitTests.Helpers;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.PortalApi.PortalUser
{
    public class MeControllerTests
    {
        private readonly Guid _searchFirmId = Guid.NewGuid();
        private readonly Guid _identityServerId = Guid.NewGuid();
        private readonly Domain.PortalUser _portalUser;
        private readonly Domain.SearchFirm _searchFirm;

        private readonly FakeRepository _fakeRepository = new FakeRepository();

        public MeControllerTests()
        {
            _searchFirm = new Domain.SearchFirm
            {
                Name = "Smith, Smith & Sons"
            };
            _searchFirmId = _searchFirm.Id;

            _portalUser = new Domain.PortalUser(_searchFirmId)
            {
                IdentityServerId = _identityServerId,
                UserName = "Paula McWilliams",
                Email = "paula@mcwilliams.com",
                SharedAssignments = new List<Domain.PortalSharedAssignment>
                {
                    new Domain.PortalSharedAssignment(Guid.NewGuid(), Guid.NewGuid()),
                    new Domain.PortalSharedAssignment(Guid.NewGuid(), Guid.NewGuid()),
                    new Domain.PortalSharedAssignment(Guid.NewGuid(), Guid.NewGuid()),
                    new Domain.PortalSharedAssignment(Guid.NewGuid(), Guid.NewGuid())
                }
            };

            var anotherPortalUser = new Domain.PortalUser(_searchFirmId)
            {
                IdentityServerId = Guid.NewGuid(),
                UserName = "Boris The Blade",
                Email = "boris@theblade.com",
                SharedAssignments = new List<Domain.PortalSharedAssignment>
                {
                    new Domain.PortalSharedAssignment(Guid.NewGuid(), Guid.NewGuid()),
                }
            };

            _fakeRepository.AddToRepository(anotherPortalUser, _portalUser, _searchFirm);
        }

        [Fact]
        public async Task ServiceReturnsPortalUser()
        {
            // Arrange
            var controller = CreateController();

            // Act
            var actionResult = await controller.Get();
            var result = (Parsnips.Application.Services.PortalUser.Model.PortalUser)((OkObjectResult)actionResult).Value;

            // Assert
            Assert.Equal(_portalUser.UserName, result.UserName);
            Assert.Equal(_portalUser.Email, result.Email);
            AssertAssignments(_portalUser.SharedAssignments, result.SharedAssignments);

            Assert.Equal(_searchFirm.Name, result.SearchFirmName);
        }

        private void AssertAssignments(List<Domain.PortalSharedAssignment> expectedAssignments, List<Parsnips.Application.Services.PortalUser.Model.PortalSharedAssignment> sharedAssignments)
        {
            if (expectedAssignments?.Any() == false && sharedAssignments?.Any() == false)
                return;

            Assert.Equal(expectedAssignments.Count(), sharedAssignments.Count());
            foreach(var expectedAssignment in expectedAssignments) 
            {
                var sharedAssignment = sharedAssignments.Single(a => a.AssignmentId == expectedAssignment.AssignmentId);
                Assert.Equal(expectedAssignment.ChangedBy, sharedAssignment.ChangedBy);
            }
        }

        private MeController CreateController()
        {
            //var config = new MapperConfiguration(cfg =>
            //    cfg.AddProfiles(new List<Profile>() { new Parsnips.Application.MappingProfile() }));

            //var autoMapper = config.CreateMapper();

            return new ControllerBuilder<MeController>()
                .SetSearchFirmUser(_searchFirmId, _identityServerId)
                .SetFakeRepository(_fakeRepository)
                //.AddTransient(autoMapper)
                .Build();
        }
    }
}
