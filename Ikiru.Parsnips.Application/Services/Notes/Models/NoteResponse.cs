using Ikiru.Parsnips.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ikiru.Parsnips.Application.Services.Notes.Models
{
    public class NoteResponse
    {
        public Note Note { get; set; }
        public SearchFirmUser CreatedBy { get; set; }
        public SearchFirmUser UpdatedBy { get; set; }
    }
}
