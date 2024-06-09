using Ikiru.Parsnips.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Security
{
    public class OwnerRequirement : IPolicyRequirement
    {
        public const string POLICY = nameof(OwnerRequirement);
        public string Policy { get; } = POLICY;
    }

    public class OwnerHandler : AuthorizationHandler<OwnerRequirement>
    {
        private readonly AuthorizationRoleHelper m_AuthorizationRoleHelper;

        public OwnerHandler(AuthorizationRoleHelper authorizationRoleHelper)
        {
            m_AuthorizationRoleHelper = authorizationRoleHelper;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, OwnerRequirement requirement)
            => await m_AuthorizationRoleHelper.SucceedIfRole(context, requirement, UserRole.Owner);
    }
}
