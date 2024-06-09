using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Ikiru.Parsnips.Api.Authentication
{
    public class AuthenticatedUserAccessor
    {
        private const string _PORTAL_CLIENT_NAME = "PortalWebAppClient";
        
        private const string _SEARCH_FIRM_ID_CLAIM = "SearchFirmId";
        private const string _USER_ID_CLAIM = "UserId";
        private const string _IDENTITYSERVERID = "IdentityServerId";
        private const string _CLIENTID = "client_id";

        private readonly IHttpContextAccessor m_HttpContextAccessor;

        public AuthenticatedUserAccessor(IHttpContextAccessor httpContextAccessor)
        {
            m_HttpContextAccessor = httpContextAccessor;
        }

        public AuthenticatedUser GetAuthenticatedUser()
        {
            var claims = GetClaims();
            return new AuthenticatedUser(claims.GetGuidClaimValue(_SEARCH_FIRM_ID_CLAIM),
                                         claims.GetGuidClaimValue(_USER_ID_CLAIM));
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

            if (userId == System.Guid.Empty)
            {
                return null;
            }

            return new AuthenticatedUser(searchFirmId: searchFirmId.Value, userId: userId.Value);
        }

        public bool IsPortalUser()
        {
            var claims = GetClaims();

            var clientId = claims.TryGetClaimValue(_CLIENTID);
            
            return clientId != null && clientId == _PORTAL_CLIENT_NAME;
        }

        public AuthenticatedPortalUser TryGetAuthenticatedPortalUser()
        {
            var claims = GetClaims();

            var clientId = claims.TryGetClaimValue(_CLIENTID);
            if (clientId == null || clientId != _PORTAL_CLIENT_NAME)
                return null;

            var searchFirmId = claims.TryGetGuidClaimValue(_SEARCH_FIRM_ID_CLAIM);
            if (searchFirmId == null)
                return null;

            var identityServerId = claims.TryGetGuidClaimValue(_IDENTITYSERVERID);
            if (identityServerId == null)
                return null;

            return new AuthenticatedPortalUser(searchFirmId.Value, identityServerId.Value);
        }

        private List<Claim> GetClaims()
            => m_HttpContextAccessor.HttpContext.User.Claims.ToList();
    }
}