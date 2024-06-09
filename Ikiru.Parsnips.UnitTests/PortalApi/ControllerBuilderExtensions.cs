using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;

namespace Ikiru.Parsnips.UnitTests.PortalApi
{
    public static class ControllerBuilderExtensions
    {
        private static ControllerBuilder<T> SetHttpContextUser<T>(this ControllerBuilder<T> builder, ClaimsPrincipal principal)
            where T : ControllerBase
        {
            builder.HttpContext.User = principal;
            return builder;
        }

        public static ControllerBuilder<T> SetSearchFirmUser<T>(this ControllerBuilder<T> builder, Guid? searchFirmId, Guid? identityUserId = null)
            where T : ControllerBase
        {
            var claims = searchFirmId == null
                             ? new Claim[0]
                             : new[]
                                 {
                                     new Claim("SearchFirmId", searchFirmId.ToString()),
                                     new Claim("UserId", Guid.Empty.ToString()),
                                     new Claim("IdentityServerId", identityUserId?.ToString() ?? Guid.Empty.ToString()),
                                 };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));
            return builder.SetHttpContextUser(principal);
        }
    }
}
