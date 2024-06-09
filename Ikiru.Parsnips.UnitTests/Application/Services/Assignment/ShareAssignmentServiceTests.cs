using AutoMapper;
using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Application.Services;
using Ikiru.Parsnips.Application.Services.Assignment;
using Ikiru.Parsnips.Application.Services.Assignment.Models;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ApiException;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ResourceNotFound;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure;
using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi;
using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi.Models;
using Ikiru.Parsnips.UnitTests.Helpers;
using Ikiru.Persistence.Repository;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Application.Services.Assignment
{
    public class ShareAssignmentServiceTests
    {
        private readonly Guid _identityServerId = Guid.NewGuid();
        private readonly Guid _identityServerIdNoPortalAssignments = Guid.NewGuid();

        private readonly string _email = "john@smith.com";
        private readonly string _existingUserName = "existing user name";
        private readonly string _existingUserName2 = "existing2 user name";

        private readonly FakeRepository _fakeRepository = new FakeRepository();
        private readonly Guid _searchFirmId = Guid.NewGuid();
        private readonly Guid _createdBy = Guid.NewGuid();
        private readonly Guid _assignmentId;
        private readonly Guid _assignmentId2;
        private readonly Guid _portalUserId;
        private readonly Guid _portalUserId2;
        private readonly Domain.PortalUser _portalUser;
        private readonly Domain.PortalUser _portalUser2;
        private readonly Domain.PortalUser _portalUserNoAssignments;
        private readonly Candidate _candidate;
        private readonly Candidate _candidate1;
        private readonly Candidate _candidate2;
        private readonly Candidate _candidate3;

        private readonly Mock<IIdentityAdminApi> _identityAdminApi = new Mock<IIdentityAdminApi>();
        private readonly CreateUserResult _createUserResult = new CreateUserResult { Id = Guid.NewGuid(), UserName = "newUserName" };
        private readonly ShareAssignmentCommand _command;

        public ShareAssignmentServiceTests()
        {
            var assignment = new Domain.Assignment(_searchFirmId);
            _assignmentId = assignment.Id;
            _candidate = new Candidate(_searchFirmId, _assignmentId, _portalUserId)
            {
                InterviewProgressState = new InterviewProgress
                {
                    Stage = Domain.Enums.CandidateStageEnum.Identified,
                    Status = Domain.Enums.CandidateStatusEnum.ArrangingInterview
                },
                ShowInClientView = true
            };

            _candidate1 = new Candidate(_searchFirmId, _assignmentId, _portalUserId)
            {
                InterviewProgressState = new InterviewProgress
                {
                    Stage = Domain.Enums.CandidateStageEnum.Identified,
                    Status = Domain.Enums.CandidateStatusEnum.ArrangingInterview
                },
                ShowInClientView = true
            };

            _candidate2 = new Candidate(_searchFirmId, _assignmentId, _portalUserId)
            {
                InterviewProgressState = new InterviewProgress
                {
                    Stage = Domain.Enums.CandidateStageEnum.ShortList,
                    Status = Domain.Enums.CandidateStatusEnum.Interested
                },
            };

            _candidate3 = new Candidate(_searchFirmId, _assignmentId, _portalUserId)
            {
                InterviewProgressState = new InterviewProgress
                {
                    Stage = Domain.Enums.CandidateStageEnum.ShortList,
                    Status = Domain.Enums.CandidateStatusEnum.ArrangingInterview
                },
                ShowInClientView = false
            };

            var assignment2 = new Domain.Assignment(_searchFirmId);
            _assignmentId2 = assignment2.Id;


            _portalUserNoAssignments = new Domain.PortalUser(_searchFirmId)
            {
                IdentityServerId = _identityServerIdNoPortalAssignments,
                UserName = _existingUserName,
                SharedAssignments = new List<PortalSharedAssignment>
                {
                }
            };

            _portalUser = new Domain.PortalUser(_searchFirmId)
            {
                Email = _email,
                IdentityServerId = _identityServerId,
                UserName = _existingUserName,
                SharedAssignments = new List<PortalSharedAssignment>
                {
                    new PortalSharedAssignment(_assignmentId,  _createdBy)
                }
            };

            _portalUser2 = new Domain.PortalUser(_searchFirmId)
            {
                UserName = _existingUserName2
            };

            _portalUserId = _portalUser.Id;

            _fakeRepository.AddToRepository(assignment, assignment2, _portalUser, _portalUser2,
                                            _candidate, _candidate1, _candidate2, _candidate3,
                                            _portalUserNoAssignments);

            _identityAdminApi
               .Setup(i => i.CreateUser(It.IsAny<CreateUserRequest>()))
               .ReturnsAsync(() => _createUserResult);

            _command = new ShareAssignmentCommand
            {
                SearchFirmId = _searchFirmId,
                UserId = _createdBy,
                AssignmentId = _assignmentId,
                Email = _email
            };
        }

        [Fact]
        public async Task ShareReturnsExistingUserIfAssignmentAlreadySharedWithThisUser()
        {
            // Arrange
            _portalUser.SharedAssignments.Add(new PortalSharedAssignment(_assignmentId, Guid.NewGuid()));
            var service = CreateService();

            // Act
            var result = await service.Share(_command);

            // Assert
            Assert.Equal(_email, result.Email);
            Assert.Equal(_existingUserName, result.UserName);
            Assert.Null(result.Password);
        }

        [Fact]
        public async Task ShareDoesNotAddAssignmentIdIfAssignmentAlreadyShared()
        {
            // Arrange
            var repositoryMock = new Mock<IRepository>();
            var assignment = new Domain.Assignment(_command.SearchFirmId);
        
            repositoryMock.Setup(r => r.GetItem<Domain.Assignment>(It.Is<string>(x => x == _command.SearchFirmId.ToString()),
                                                                    It.Is<string>(a => a == _command.AssignmentId.ToString())))
                            .Returns(Task.FromResult(assignment));

            repositoryMock.Setup(x => x.GetByQuery(It.IsAny<string>(),
                                                It.IsAny<Expression<Func<IOrderedQueryable<Domain.PortalUser>, IQueryable<Domain.PortalUser>>>>(), null))
                .Returns(Task.FromResult(new List<Domain.PortalUser>()
                {
                    new Domain.PortalUser(_command.SearchFirmId)
                    {
                        SharedAssignments = new List<PortalSharedAssignment>()
                        {
                            new PortalSharedAssignment(_command.AssignmentId, Guid.NewGuid())
                        }
                    }
                }));

            var noteServiceMock = new Mock<INoteService>();
            var identityAdminApiMock = new Mock<IIdentityAdminApi>();
            var loggerMock = new Mock<ILogger<AssignmentService>>();
            var config = new MapperConfiguration(cfg => cfg.AddProfile<Parsnips.Application.MappingProfile>());
            var mapper = config.CreateMapper();

            var service = new AssignmentService(new AssignmentRepository(repositoryMock.Object),
                                                new PortalUserRepository(repositoryMock.Object),
                                                noteServiceMock.Object,
                                                mapper,
                                                identityAdminApiMock.Object,
                                                loggerMock.Object,
                                                new CandidateRepository(repositoryMock.Object), null, null, null);

            // Act
            var result = await service.Share(_command);

            // Assert
             repositoryMock.Verify(p => p.UpdateItem(It.IsAny<Domain.PortalUser>()), Times.Never);
        }

        [Fact]
        public async Task ShareCreatesNewUser()
        {
            const string newUserEmail = "newuser@email.com";

            // Arrange
            _command.Email = newUserEmail;
            var service = CreateService();

            // Act
            await service.Share(_command);

            // Assert
            _identityAdminApi.Verify(i => i.CreateUser(It.Is<CreateUserRequest>(r =>
                                                                                    r.EmailAddress == newUserEmail &&
                                                                                    r.Password.Length == 10 &&
                                                                                    r.SearchFirmId == _searchFirmId &&
                                                                                    r.UserId == Guid.Empty &&
                                                                                    r.IsDisabled == false &&
                                                                                    r.GenerateUniqueUserName &&
                                                                                    r.BypassConfirmEmailAddress)));
        }

        [Fact]
        public async Task ShareAddsAssignmentIdIfNoAssignment()
        {
            // Arrange
            var service = CreateService();

            // Act
            await service.Share(_command);

            // Assert
            var portalUser = await _fakeRepository.GetItem<Domain.PortalUser>(_searchFirmId.ToString(), _portalUserId.ToString());
            var assignments = portalUser.SharedAssignments.Where(a => a.AssignmentId == _assignmentId).ToList();
            Assert.Single(assignments);
            Assert.Equal(_createdBy, assignments.Single().ChangedBy);
        }

        [Fact]
        public async Task ShareReturnsCorrectPortalUser()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = await service.Share(_command);

            // Assert
            Assert.Equal(_email, result.Email);
            Assert.Equal(_existingUserName, result.UserName);
            Assert.Null(result.Password);
        }

        [Fact]
        public async Task ShareThrowsWhenNoAssignment()
        {
            // Arrange
            _command.AssignmentId = Guid.NewGuid();
            var service = CreateService();

            // Act
            var ex = await Record.ExceptionAsync(() => service.Share(_command));

            // Assert
            Assert.NotNull(ex);
            Assert.IsType<ResourceNotFoundException>(ex);
        }

        [Fact]
        public async Task ShareThrowsWhenUserIsNotCreated()
        {
            const string nonExistingUser = "nonexisting@email.com";

            // Arrange
            _command.Email = nonExistingUser;
            _identityAdminApi
               .Setup(i => i.CreateUser(It.Is<CreateUserRequest>(r => r.EmailAddress == nonExistingUser)))
               .ReturnsAsync(() => null);
            var service = CreateService();

            // Act
            var ex = await Record.ExceptionAsync(() => service.Share(_command));

            // Assert
            Assert.NotNull(ex);
            Assert.IsType<ExternalApiException>(ex);
        }

        [Fact]
        public async Task ShareThrowsWhenIdentityThrows()
        {
            const string nonExistingUser = "nonexisting@email.com";

            // Arrange
            _command.Email = nonExistingUser;
            _identityAdminApi
               .Setup(i => i.CreateUser(It.Is<CreateUserRequest>(r => r.EmailAddress == nonExistingUser)))
               .ThrowsAsync(new Exception("cannot create user"));
            var service = CreateService();

            // Act
            var ex = await Record.ExceptionAsync(() => service.Share(_command));

            // Assert
            Assert.NotNull(ex);
            Assert.IsType<ExternalApiException>(ex);
        }

        [Fact]
        public async Task ServiceThrowsWhenValidationFails()
        {
            const string badEmail = "badlyFormedEmail.com";

            // Arrange
            _command.Email = badEmail;
            var service = CreateService();

            // Act
            var ex = await Record.ExceptionAsync(() => service.Share(_command));

            // Assert
            Assert.NotNull(ex);
            Assert.IsType<ParamValidationFailureException>(ex);
        }

        [Fact]
        public async Task GetSharedAssignmentsForClientAndListCandidatesSelectedToBeShared()
        {
            // Arrange
            var service = CreateService();

            // Act 
            var result = await service.GetSharedAssignmentsForClient(searchFirmId: _searchFirmId,
                                                                    identityServerId: _identityServerId);

            // Assert
            Assert.Single(result.SharedAssignmentDetails);
            Assert.Equal(_email, result.ClientEmail);
            Assert.True(result.SharedAssignmentDetails.Exists(x => x.AssignmentId == _assignmentId));
            Assert.False(result.SharedAssignmentDetails.Exists(x => x.AssignmentId == _assignmentId2));
            Assert.Single(result.SharedAssignmentDetails);
            Assert.Equal(0, result.SharedAssignmentDetails[0].AssignmentCandidateStageCount.ShortList);
            Assert.Equal(2, result.SharedAssignmentDetails[0].AssignmentCandidateStageCount.Identified);
        }

        [Fact]
        public async Task GetSharedAssignmentsForClientThrowsWhenNoPortalAssignmentFound()
        {
            // Arrange
            var service = CreateService();

            // Act 
            var result = await Record.ExceptionAsync(() => service.GetSharedAssignmentsForClient(Guid.NewGuid(), Guid.NewGuid()));

            // Assert
            Assert.Equal("portal assignment does not exists", result.Message);
        }

        [Fact]
        public async Task GetSharedAssignmentsForClientThrowsWhenNoPortalAssignmentsShared()
        {
            // Arrange
            var service = CreateService();

            // Act 
            var result = await Record.ExceptionAsync(() => service.GetSharedAssignmentsForClient(searchFirmId: _searchFirmId,
                                                                                                identityServerId: _identityServerIdNoPortalAssignments));

            // Assert
            Assert.Equal("no portal assignments shared", result.Message);
        }

        private AssignmentService CreateService()
        {
            return new ServiceBuilder<AssignmentService>()
                  .SetFakeRepository(_fakeRepository)
                  .AddTransient(_identityAdminApi.Object)
                  .Build();
        }
    }
}