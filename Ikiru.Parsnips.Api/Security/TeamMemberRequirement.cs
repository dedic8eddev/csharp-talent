using Ikiru.Parsnips.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Security
{
    public class TeamMemberRequirement : IPolicyRequirement
    {
        public const string POLICY = nameof(TeamMemberRequirement);
        public string Policy { get; } = POLICY;
    }

    public class TeamMemberHandler : AuthorizationHandler<TeamMemberRequirement>
    {
        private readonly AuthorizationRoleHelper m_AuthorizationRoleHelper;

        public TeamMemberHandler(AuthorizationRoleHelper authorizationRoleHelper)
        {
            m_AuthorizationRoleHelper = authorizationRoleHelper;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, TeamMemberRequirement requirement)
            => await m_AuthorizationRoleHelper.SucceedIfRole(context, requirement, UserRole.TeamMember);
    }
}
