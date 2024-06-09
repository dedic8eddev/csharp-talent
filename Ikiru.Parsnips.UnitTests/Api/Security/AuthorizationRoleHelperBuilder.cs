using Ikiru.Parsnips.Api.Authentication;
using Ikiru.Parsnips.Api.Security;
using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.UnitTests.Helpers;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace Ikiru.Parsnips.UnitTests.Api.Security
{
    public class AuthorizationRoleHelperBuilder
    {
        private HttpContext HttpContext { get; } = new DefaultHttpContext();
        private FakeRepository FakeRepository { get; set; } = new FakeRepository();

        public AuthorizationRoleHelperBuilder()
        {
            SetEmptySearchFirmUser();
        }

        internal AuthorizationRoleHelper Build()
        {
            var httpContextAccessor = Mock.Of<IHttpContextAccessor>(a => a.HttpContext == HttpContext);

            var authenticatedUserAccessor = new AuthenticatedUserAccessor(httpContextAccessor);

            return new AuthorizationRoleHelper(authenticatedUserAccessor, new UserRepository(FakeRepository));
        }

        internal void AddFakeRepository(FakeRepository fakeRepository) => FakeRepository = fakeRepository;

        internal void SetSearchFirmUser(Guid searchFirmId, Guid userId)
        {
            var claims = SetClaims(searchFirmId, userId);
            SetSearchFirmUser(claims);
        }

        internal void SetPortalUser(Guid searchFirmId, Guid userId, Guid identityServerId)
        {
            var clientId = "PortalWebAppClient";

            var claims = SetClaims(searchFirmId, userId, identityServerId, clientId);
            SetSearchFirmUser(claims);
        }

        private List<Claim> SetClaims(Guid searchFirmId, Guid? userId = null, Guid? identityServerId = null, string clientId = null)
        {
            var claims = new List<Claim>
            {
                new Claim("SearchFirmId", searchFirmId.ToString())
            };

            if (userId != null)
                claims.Add(new Claim("UserId", userId.ToString()));

            if (identityServerId != null)
                claims.Add(new Claim("IdentityServerId", identityServerId.ToString()));

            if (clientId != null)
                claims.Add(new Claim("client_id", clientId));

            return claims;
        }

        private void SetEmptySearchFirmUser() => SetSearchFirmUser(new List<Claim>());
        private void SetSearchFirmUser(List<Claim> claims)
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));
            HttpContext.User = principal;
        }
    }
}
