using System;

namespace Ikiru.Parsnips.Api.Authentication
{
    public class AuthenticatedPortalUser
    {
        public Guid SearchFirmId { get; }
        public Guid IdentityServerId { get; }

        public AuthenticatedPortalUser(Guid searchFirmId, Guid identityServerId)
        {
            SearchFirmId = searchFirmId;
            IdentityServerId = identityServerId;
        }
    }
}