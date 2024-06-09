using Ikiru.Parsnips.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Security
{
    public class AdminRequirement : IPolicyRequirement
    {
        public const string POLICY = nameof(AdminRequirement);
        public string Policy { get; } = POLICY;
    }

    public class AdminHandler : AuthorizationHandler<AdminRequirement>
    {
        private readonly AuthorizationRoleHelper m_AuthorizationRoleHelper;

        public AdminHandler(AuthorizationRoleHelper authorizationRoleHelper)
        {
            m_AuthorizationRoleHelper = authorizationRoleHelper;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminRequirement requirement)
            => await m_AuthorizationRoleHelper.SucceedIfRole(context, requirement, UserRole.Admin);
    }
}