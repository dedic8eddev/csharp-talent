using Ikiru.Parsnips.Api.Security;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.UnitTests.Helpers;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Security
{
    public class AdminRequirementTests
    {
        private readonly SearchFirm m_SearchFirm = new SearchFirm { IsEnabled = true };
        private readonly SearchFirm m_DisabledSearchFirm = new SearchFirm();
        private readonly SearchFirmUser m_User;
        private readonly SearchFirmUser m_DisabledAgencyUser;
        private readonly AuthorizationRoleHelperBuilder m_ServiceBuilder;
        private readonly AdminRequirement m_Requirement = new AdminRequirement();

        private AuthorizationHandlerContext m_Context;

        public AdminRequirementTests()
        {
            m_User = new SearchFirmUser(m_SearchFirm.Id);
            m_DisabledAgencyUser = new SearchFirmUser(m_DisabledSearchFirm.Id);

            var fakeRepository = new FakeRepository();
            fakeRepository.AddToRepository(m_User, m_DisabledAgencyUser, m_SearchFirm, m_DisabledSearchFirm);

            m_ServiceBuilder = new AuthorizationRoleHelperBuilder();
            m_ServiceBuilder.AddFakeRepository(fakeRepository);
        }

        public static IEnumerable<object[]> AuthorizationTestData()
        {
            yield return new object[] { UserRole.Owner, true };
            yield return new object[] { UserRole.Admin, true };
            yield return new object[] { UserRole.TeamMember, false };
        }

        [Theory]
        [MemberData(nameof(AuthorizationTestData))]
        public async Task AuthorizationSucceedsIfCorrectRole(UserRole userAuthorizationRole, bool expectedHasSucceeded)
        {
            // Given
            m_ServiceBuilder.SetSearchFirmUser(m_SearchFirm.Id, m_User.Id);
            m_User.UserRole = userAuthorizationRole;
            var handler = CreateHandler();

            // When
            await handler.TestHandleRequirementAsync(m_Context, m_Requirement);

            // Then
            Assert.Equal(expectedHasSucceeded, m_Context.HasSucceeded);
        }

        private TestHandler CreateHandler()
        {
            var requirements = new List<IAuthorizationRequirement> { m_Requirement };
            m_Context = new AuthorizationHandlerContext(requirements, new ClaimsPrincipal(), null);

            var authorizationRoleHelper = m_ServiceBuilder.Build();

            return new TestHandler(authorizationRoleHelper);
        }

        // An easy way to test protected method without figuring out how it is called from public
        private class TestHandler : AdminHandler
        {
            public TestHandler(AuthorizationRoleHelper authorizationRoleHelper) : base(authorizationRoleHelper)
            {
            }

            public Task TestHandleRequirementAsync(AuthorizationHandlerContext context, AdminRequirement requirement)
                => HandleRequirementAsync(context, requirement);
        }
    }
}
