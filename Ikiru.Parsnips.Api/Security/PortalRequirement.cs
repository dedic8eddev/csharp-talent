using Ikiru.Parsnips.Api.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Security
{
    public class PortalRequirement : IPolicyRequirement
    {
        public const string POLICY = nameof(PortalRequirement);
        public string Policy { get; } = POLICY;
    }

    public class PortalHandler : AuthorizationHandler<PortalRequirement>
    {
        private readonly AuthenticatedUserAccessor _authenticatedUserAccessor;

        public PortalHandler(AuthenticatedUserAccessor authenticatedUserAccessor)
        {
            _authenticatedUserAccessor = authenticatedUserAccessor;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PortalRequirement requirement)
        {
            if (!_authenticatedUserAccessor.IsPortalUser())
                return Task.CompletedTask;

            context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }
}
