using System;
using System.Collections.Generic;
using System.Text;

namespace Ikiru.Parsnips.Application.Query.Users.Models
{
    public class GetUserDetailsByUserIdRequest
    {
        public Guid UserId { get; set; }
        public Guid SearchFirmId { get; set; }
    }
}
