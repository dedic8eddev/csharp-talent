using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Ikiru.Parsnips.Portal.Api.Authentication
{
    public class AuthenticatedUserAccessor
    {
        private readonly IHttpContextAccessor m_HttpContextAccessor;

        private const string _SEARCH_FIRM_ID_CLAIM = "SearchFirmId";
        private const string _USER_ID_CLAIM = "UserId";
        private const string _IDENTITYSERVERID = "IdentityServerId";

        public AuthenticatedUserAccessor(IHttpContextAccessor httpContextAccessor)
        {
            m_HttpContextAccessor = httpContextAccessor;
        }

        public AuthenticatedUser GetAuthenticatedUser()
        {
            var claims = GetClaims();
            return new AuthenticatedUser(claims.GetGuidClaimValue(_SEARCH_FIRM_ID_CLAIM),
                                         claims.GetGuidClaimValue(_USER_ID_CLAIM),
                                         claims.GetGuidClaimValue(_IDENTITYSERVERID));
        }

        public AuthenticatedUser TryGetAuthenticatedUser()
        {
            var claims = GetClaims();
            var searchFirmId = claims.TryGetGuidClaimValue(_SEARCH_FIRM_ID_CLAIM);
            if (searchFirmId == null)
                return null;

            var userId = claims.TryGetGuidClaimValue(_USER_ID_CLAIM);
            if (userId == null)
                return null;

            var identityServerId = claims.TryGetGuidClaimValue(_IDENTITYSERVERID);
            if (identityServerId == null)
                return null;

            return new AuthenticatedUser(searchFirmId: searchFirmId.Value, userId: userId.Value, identityServerId: identityServerId.Value);
        }

        private List<Claim> GetClaims()
            => m_HttpContextAccessor.HttpContext.User.Claims.ToList();
    }
}