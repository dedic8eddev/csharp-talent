using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Assignments.Notes.Models
{
    public class UpdateNoteModel
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public Guid PersonId { get; set; }
        public DateTimeOffset UpdatedDate{ get; set; }
    }
}
