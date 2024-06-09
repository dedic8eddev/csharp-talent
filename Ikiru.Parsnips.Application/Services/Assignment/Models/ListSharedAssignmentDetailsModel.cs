using System;
using System.Collections.Generic;
using System.Text;

namespace Ikiru.Parsnips.Application.Services.Assignment.Models
{
    public class ListSharedAssignmentDetailsModel
    {
        public string ClientEmail { get; set; }
        public List<SharedAssignmentDetailsModel> SharedAssignmentDetails { get; set; }
    }
}
