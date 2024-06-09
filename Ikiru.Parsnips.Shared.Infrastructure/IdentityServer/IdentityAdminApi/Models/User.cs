using System;

namespace Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string EmailAddress { get; set; }
        public Guid SearchFirmId { get; set; }
        public Guid UserId { get; set; }
        public bool IsDisabled { get; set; }
        public bool EmailConfirmed { get; set; }
    }
}
