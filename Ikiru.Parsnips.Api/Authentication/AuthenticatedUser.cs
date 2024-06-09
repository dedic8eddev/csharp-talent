using System;

namespace Ikiru.Parsnips.Api.Authentication
{
    public class AuthenticatedUser
    {
        public Guid SearchFirmId { get; }
        public Guid UserId { get; }

        public AuthenticatedUser(Guid searchFirmId, Guid userId)
        {
            SearchFirmId = searchFirmId;
            UserId = userId;
        }
    }
}