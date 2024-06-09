using System;
using System.Collections.Generic;
using System.Text;

namespace Ikiru.Parsnips.Application.Command.Users.Models
{
    public class MakeUserActiveRequest
    {
        public Guid SearchFirmId { get; set; }
        public Guid SearchFirmUserIdLoggedIn { get; set; }
        public Guid SearchFirmUserIdToMakeActive { get; set; }
    }
}
