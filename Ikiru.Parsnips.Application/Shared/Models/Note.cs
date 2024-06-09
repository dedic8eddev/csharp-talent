using System;

namespace Ikiru.Parsnips.Application.Shared.Models
{
        public class Note
        {
            public string NoteTitle { get; set; }
            public string ByFirstName { get; set; }
            public string ByLastName { get; set; }
            public DateTimeOffset CreatedOrUpdated { get; set; }
        }
}
