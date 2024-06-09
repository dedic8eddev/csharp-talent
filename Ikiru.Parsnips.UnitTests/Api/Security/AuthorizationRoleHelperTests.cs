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
    public class AuthorizationRoleHelperTests
    {
        private readonly Dictionary<UserRole, IPolicyRequirement> m_RequirementsConverter;

        private readonly List<IAuthorizationRequirement> m_Requirements;
        private readonly Guid m_SearchFirmId = Guid.NewGuid();
        private readonly AuthorizationRoleHelperBuilder m_ServiceBuilder;

        private AuthorizationHandlerContext m_Context;

        public AuthorizationRoleHelperTests()
        {
            var owner = new OwnerRequirement();
            var admin = new AdminRequirement();

            m_RequirementsConverter = new Dictionary<UserRole, IPolicyRequirement>
                                      {
                                          { UserRole.Owner, owner },
                                          { UserRole.Admin, admin },
                                      };
            m_Requirements = new List<IAuthorizationRequirement> { owner, admin };

            var user = new SearchFirmUser(m_SearchFirmId);

            var fakeRepository = new FakeRepository();
            fakeRepository.AddToRepository(user);

            m_ServiceBuilder = new AuthorizationRoleHelperBuilder();
            m_ServiceBuilder.AddFakeRepository(fakeRepository);
        }

        [Theory, CombinatorialData]
        public async Task AuthorizationFailsIfUnauthorized
            ([CombinatorialValues(UserRole.Owner, UserRole.Admin)] UserRole requiredAuthorizationRole)
        {
            // Given
            var requirement = m_RequirementsConverter[requiredAuthorizationRole];
            var helper = CreateAuthorizationRoleHelper();

            // When
            await helper.SucceedIfRole(m_Context, requirement, requiredAuthorizationRole);

            // Then
            Assert.False(m_Context.HasSucceeded);
        }

        [Theory, CombinatorialData]
        public async Task AuthorizationFailsIfPortalUser
            ([CombinatorialValues(UserRole.Owner, UserRole.Admin)] UserRole requiredAuthorizationRole)
        {
            // Given
            var requirement = m_RequirementsConverter[requiredAuthorizationRole];
            
            m_ServiceBuilder.SetPortalUser(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            var helper = CreateAuthorizationRoleHelper();

            // When
            await helper.SucceedIfRole(m_Context, requirement, requiredAuthorizationRole);

            // Then
            Assert.False(m_Context.HasSucceeded);
        }

        private AuthorizationRoleHelper CreateAuthorizationRoleHelper()
        {
            m_Context = new AuthorizationHandlerContext(m_Requirements, new ClaimsPrincipal(), null);

            return m_ServiceBuilder.Build();
        }
    }
}
