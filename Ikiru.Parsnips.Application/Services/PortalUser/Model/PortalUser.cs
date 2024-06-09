using System.Collections.Generic;

namespace Ikiru.Parsnips.Application.Services.PortalUser.Model
{
    public class PortalUser
    {
        public IEnumerable<char> UserName { get; set; }
        public IEnumerable<char> Email { get; set; }
        public List<PortalSharedAssignment> SharedAssignments { get; set; }

        public string SearchFirmName { get; set; }
    }
}
