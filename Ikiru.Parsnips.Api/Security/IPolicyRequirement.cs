using Microsoft.AspNetCore.Authorization;

namespace Ikiru.Parsnips.Api.Security
{
    public interface IPolicyRequirement : IAuthorizationRequirement
    {
        public string Policy { get; }
    }
}
