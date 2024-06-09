using Ikiru.Parsnips.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Users.Invite.Models
{
    public class MultipleInvitesModel
    {
        public string Email { get; set; }
        public UserRole UserRole { get; set; }
    }
}
