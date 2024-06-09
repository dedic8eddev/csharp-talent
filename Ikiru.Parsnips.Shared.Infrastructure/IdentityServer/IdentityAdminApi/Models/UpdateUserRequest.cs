using System;

namespace Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi.Models
{
    public class UpdateUserRequest
    {
        public string Password { get; set; }
        public bool? EmailConfirmed { get; set; }
        public bool? UnlockAccount { get; set; }
        public bool? DisableLogin { get; set; }
        public DateTimeOffset? DisableLoginEndDate { get; set; } = null;
    }
}
