using Ikiru.Parsnips.Api.Controllers.Users;
using Ikiru.Parsnips.Application.Command;
using Ikiru.Parsnips.Application.Command.Users.Models;
using Ikiru.Parsnips.Application.Query;
using Ikiru.Parsnips.Application.Query.Users.Models;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.UnitTests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Users
{
    public class GetListTests
    {
        private readonly Guid m_SearchFirmId = Guid.NewGuid();
        private readonly FakeCosmos m_FakeCosmos = new FakeCosmos();
        private readonly List<SearchFirmUser> m_SearchFirmUsers = new List<SearchFirmUser>();
        private readonly SearchFirmUser m_Admin;
        private readonly Mock<ICommandHandler<MakeUserInActiveRequest, MakeUserInActiveResponse>> _makeUserInActiveMock;
        private readonly Mock<ICommandHandler<MakeUserActiveRequest, MakeUserActiveResponse>> _makeUserActiveMock;
        private readonly Mock<IQueryHandler<GetActiveUsersRequest, GetActiveUsersResponse>> _userQuery;


        public GetListTests()
        {
            _userQuery = new Mock<IQueryHandler<GetActiveUsersRequest, GetActiveUsersResponse>>();
            _makeUserInActiveMock = new Mock<ICommandHandler<MakeUserInActiveRequest, MakeUserInActiveResponse>>();
            _makeUserActiveMock = new Mock<ICommandHandler<MakeUserActiveRequest, MakeUserActiveResponse>>();

            m_Admin = new SearchFirmUser(m_SearchFirmId) //first in list
            {
                FirstName = "Super",
                LastName = "admin",
                EmailAddress = "super@admin.com",
                JobTitle = "super admin",
                UserRole = UserRole.Owner
            };

            m_SearchFirmUsers.Add(
                                  new SearchFirmUser(m_SearchFirmId) //4th in list
                                  {
                                      FirstName = "Ordinary",
                                      LastName = "Admin",
                                      EmailAddress = "ordinary@manager.com",
                                      JobTitle = "just a manager",
                                      Status = SearchFirmUserStatus.Invited,
                                      InvitedBy = m_Admin.Id,
                                      UserRole = UserRole.Admin
                                  });

            m_SearchFirmUsers.Add(
                                  new SearchFirmUser(m_SearchFirmId) //3rd in list
                                  {
                                      FirstName = "Ordinary",
                                      LastName = "Absolutely",
                                      EmailAddress = "absolutely@manager.com",
                                      JobTitle = "absolutely ordinary manager",
                                      Status = SearchFirmUserStatus.Invited,
                                      InvitedBy = m_Admin.Id,
                                      UserRole = UserRole.Admin
                                  });

            m_SearchFirmUsers.Add(
                                  new SearchFirmUser(m_SearchFirmId) //2nd in list
                                  {
                                      FirstName = "Normal",
                                      LastName = "Absolutely",
                                      EmailAddress = "absolutely@Normal.com",
                                      JobTitle = "absolutely normal manager",
                                      Status = SearchFirmUserStatus.Invited,
                                      InvitedBy = m_Admin.Id,
                                      UserRole = UserRole.TeamMember
                                  });
            m_SearchFirmUsers.Add(m_Admin);

            m_FakeCosmos.EnableContainerLinqQuery
                (FakeCosmos.SearchFirmsContainerName, m_SearchFirmId.ToString(), () => m_SearchFirmUsers);
        }

        [Fact]
        public async Task GetListReturnsCorrectSortedResult()
        {
            // Given
            var controller = CreateController();

            // When
            var actionResult = await controller.GetList();

            // Then
            var result = (GetList.Result)((OkObjectResult)actionResult).Value;
            AssertUser(m_SearchFirmUsers[0], m_Admin, result.Users[3]);
            AssertUser(m_SearchFirmUsers[1], m_Admin, result.Users[2]);
            AssertUser(m_SearchFirmUsers[2], m_Admin, result.Users[1]);
            AssertUser(m_SearchFirmUsers[3], null, result.Users[0]);
        }

        private static void AssertUser(SearchFirmUser expectedUser, SearchFirmUser invitedBy, GetList.Result.UserDetails resultUser)
        {
            Assert.Equal(expectedUser.Id, resultUser.Id);
            Assert.Equal(expectedUser.FirstName, resultUser.FirstName);
            Assert.Equal(expectedUser.LastName, resultUser.LastName);
            Assert.Equal(expectedUser.EmailAddress, resultUser.EmailAddress);
            Assert.Equal(expectedUser.JobTitle, resultUser.JobTitle);
            Assert.Equal(expectedUser.Status, resultUser.Status);
            Assert.Equal(expectedUser.UserRole, resultUser.UserRole);

            if (invitedBy == null)
                return;

            Assert.Equal(invitedBy.FirstName, resultUser.InvitedBy.FirstName);
            Assert.Equal(invitedBy.LastName, resultUser.InvitedBy.LastName);
            Assert.Equal(invitedBy.EmailAddress, resultUser.InvitedBy.EmailAddress);
            Assert.Equal(invitedBy.JobTitle, resultUser.InvitedBy.JobTitle);
        }

        private UsersController CreateController()
        {
            return new ControllerBuilder<UsersController>()
                  .SetFakeCosmos(m_FakeCosmos)
                  .SetSearchFirmUser(m_Admin.SearchFirmId, m_Admin.Id)
                  .AddTransient(_makeUserActiveMock.Object)
                  .AddTransient(_makeUserInActiveMock.Object)
                  .AddTransient(_userQuery.Object)
                  .Build();
        }
    }
}
