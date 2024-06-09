using Ikiru.Parsnips.Api.Controllers.Users;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure;
using Ikiru.Parsnips.UnitTests.Helpers;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Users
{
    public class PutTests
    {
        private const string _lastName = "Bon";
        private const string _jobTitle = "CEO";
        private const string _firstName = "John";

        private readonly Guid m_SearchFirmId = Guid.NewGuid();

        private readonly SearchFirmUser _loggedInUser;
        private readonly SearchFirmUser _userToChange;

        private readonly FakeRepository _fakeRepository = new FakeRepository();
        private readonly Put.Command _command;

        public PutTests()
        {
            _loggedInUser = new SearchFirmUser(m_SearchFirmId) { UserRole = UserRole.Owner };

            _userToChange = new SearchFirmUser(m_SearchFirmId)
            {
                FirstName = _firstName,
                LastName = _lastName,
                UserRole = UserRole.TeamMember,
                JobTitle = _jobTitle
            };

            _loggedInUser = new SearchFirmUser(m_SearchFirmId)
            {
                FirstName = "Logged",
                LastName = "In",
                UserRole = UserRole.Owner,
                JobTitle = "CEO"
            };

            _fakeRepository.AddToRepository(_userToChange, _loggedInUser);

            _command = new Put.Command
            {
                Id = _userToChange.Id,
                UserRole = UserRole.TeamMember
            };
        }

        [Theory]
        [InlineData(UserRole.Owner, UserRole.Owner, UserRole.Admin)]
        [InlineData(UserRole.Owner, UserRole.Owner, UserRole.TeamMember)]
        [InlineData(UserRole.Owner, UserRole.Admin, UserRole.Owner)]
        [InlineData(UserRole.Owner, UserRole.Admin, UserRole.TeamMember)]
        [InlineData(UserRole.Owner, UserRole.TeamMember, UserRole.Owner)]
        [InlineData(UserRole.Owner, UserRole.TeamMember, UserRole.Admin)]
        [InlineData(UserRole.Admin, UserRole.Admin, UserRole.TeamMember)]
        [InlineData(UserRole.Admin, UserRole.TeamMember, UserRole.Admin)]
        public async Task PutUpdatesUser(UserRole changerRole, UserRole changeeFromRole, UserRole changeeToRole)
        {
            // Given
            _loggedInUser.UserRole = changerRole;
            _userToChange.UserRole = changeeFromRole;
            _command.UserRole = changeeToRole;
            var controller = CreateController();

            // When
            var actionResult = await controller.Put(_command);

            // Then
            var resultUser = (Put.Result)((OkObjectResult)actionResult).Value;
            Assert.Equal(_command.Id, resultUser.Id);
            Assert.Equal(changeeToRole, resultUser.UserRole);

            var saved = await _fakeRepository.GetItem<SearchFirmUser>(m_SearchFirmId.ToString(), _command.Id.ToString());
            Assert.NotNull(saved);
            Assert.Equal(_command.Id, saved.Id);
            Assert.Equal(changeeToRole, saved.UserRole);
            Assert.Equal(_firstName, saved.FirstName);
            Assert.Equal(_lastName, saved.LastName);
            Assert.Equal(_jobTitle, saved.JobTitle);
        }

        [Theory]
        [InlineData(UserRole.Admin, UserRole.Owner, UserRole.TeamMember)]
        [InlineData(UserRole.Admin, UserRole.Owner, UserRole.Admin)]
        [InlineData(UserRole.Admin, UserRole.Admin, UserRole.Owner)]
        [InlineData(UserRole.Admin, UserRole.TeamMember, UserRole.Owner)]
        [InlineData(UserRole.TeamMember, UserRole.Owner, UserRole.Admin)]
        [InlineData(UserRole.TeamMember, UserRole.Owner, UserRole.TeamMember)]
        [InlineData(UserRole.TeamMember, UserRole.Admin, UserRole.Owner)]
        [InlineData(UserRole.TeamMember, UserRole.Admin, UserRole.TeamMember)]
        [InlineData(UserRole.TeamMember, UserRole.TeamMember, UserRole.Owner)]
        [InlineData(UserRole.TeamMember, UserRole.TeamMember, UserRole.Admin)]
        public async Task PutDoesNotUpdateUserIfWrongRoles(UserRole changerRole, UserRole changeeFromRole, UserRole changeeToRole)
        {
            // Given
            _loggedInUser.UserRole = changerRole;
            _userToChange.UserRole = changeeFromRole;
            _command.UserRole = changeeToRole;
            var controller = CreateController();

            // When
            var ex = await Record.ExceptionAsync(() => controller.Put(_command));

            // Then
            Assert.NotNull(ex);
            Assert.IsType<ParamValidationFailureException>(ex);

            var existing = await _fakeRepository.GetItem<SearchFirmUser>(m_SearchFirmId.ToString(), _command.Id.ToString());
            Assert.NotNull(existing);
            Assert.Equal(_command.Id, existing.Id);
            Assert.Equal(changeeFromRole, existing.UserRole);
            Assert.Equal(_firstName, existing.FirstName);
            Assert.Equal(_lastName, existing.LastName);
            Assert.Equal(_jobTitle, existing.JobTitle);
        }

        [Theory]
        [InlineData(UserRole.Admin)]
        [InlineData(UserRole.TeamMember)]
        public async Task PutDoesNotUpdateUserIfOwnerDemotesThemSelves(UserRole changerRole)
        {
            // Given
            _loggedInUser.UserRole = UserRole.Owner;
            _command.Id = _loggedInUser.Id;
            _command.UserRole = changerRole;
            var controller = CreateController();

            // When
            var ex = await Record.ExceptionAsync(() => controller.Put(_command));

            // Then
            Assert.NotNull(ex);
            Assert.IsType<ParamValidationFailureException>(ex);

            var existing = await _fakeRepository.GetItem<SearchFirmUser>(m_SearchFirmId.ToString(), _command.Id.ToString());
            Assert.NotNull(existing);
            Assert.Equal(_command.Id, existing.Id);
            Assert.Equal(UserRole.Owner, existing.UserRole);
        }



        private UsersController CreateController()
        {
            return new ControllerBuilder<UsersController>()
                  .SetSearchFirmUser(m_SearchFirmId, _loggedInUser.Id)
                  .SetFakeRepository(_fakeRepository)
                  .Build();
        }
    }
}
