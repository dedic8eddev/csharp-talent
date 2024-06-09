using System;

namespace Ikiru.Parsnips.Portal.Api.Authentication
{
    public class AuthenticatedUser
    {
        public Guid SearchFirmId { get; }
        public Guid UserId { get; }
        public Guid IdentityServerId { get; }

        public AuthenticatedUser(Guid searchFirmId, Guid userId, Guid identityServerId)
        {
            SearchFirmId = searchFirmId;
            UserId = userId;
            IdentityServerId = identityServerId;
        }
    }
}