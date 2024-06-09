using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Assignments.Notes.Models
{
    public class CreateNoteModel
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTimeOffset CreatedDate{ get; set; }
    }
}
