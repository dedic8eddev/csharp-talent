using System;

namespace Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi.Models
{
    public class CreateUserResult 
    {
        public Guid Id { get; set; }
        public string UserName { get; set; }
    }
}