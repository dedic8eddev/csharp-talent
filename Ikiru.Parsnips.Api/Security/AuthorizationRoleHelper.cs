using Ikiru.Parsnips.Api.Authentication;
using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Security
{
    public class AuthorizationRoleHelper
    {
        private readonly AuthenticatedUserAccessor m_AuthenticatedUserAccessor;
        private readonly UserRepository m_UserRepository;

        public AuthorizationRoleHelper(AuthenticatedUserAccessor authenticatedUserAccessor, UserRepository userRepository)
        {
            m_AuthenticatedUserAccessor = authenticatedUserAccessor;
            m_UserRepository = userRepository;
        }

        public async Task SucceedIfRole(AuthorizationHandlerContext context, IAuthorizationRequirement requirement, UserRole requiredRole)
        {
            if (m_AuthenticatedUserAccessor.IsPortalUser())
                return;

            var authenticatedUser = m_AuthenticatedUserAccessor.TryGetAuthenticatedUser();

            if (authenticatedUser == null)
                return;

            var user = await m_UserRepository.GetUserById(authenticatedUser.UserId, authenticatedUser.SearchFirmId);

            // Owner can do everything, Admin can do actions allowed to Admin&TeamMember, TeamMember can do actions allowed to TeamMember
            // Change the logic here if move to group based access control
            if ((int)user.UserRole >= (int)requiredRole)
                context.Succeed(requirement);
        }
    }
}
