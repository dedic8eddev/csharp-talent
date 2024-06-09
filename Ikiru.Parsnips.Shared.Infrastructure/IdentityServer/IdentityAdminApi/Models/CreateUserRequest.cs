using System;

namespace Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi.Models
{
    public class CreateUserRequest
    {
        public string EmailAddress { get; set; }
        public string Password { get; set; }
        public Guid SearchFirmId { get; set; }
        public Guid UserId { get; set; }
        public bool IsDisabled { get; set; } = false;
        public bool GenerateUniqueUserName { get; set; } = false;
        public bool BypassConfirmEmailAddress { get; set; } = false;
    }
}