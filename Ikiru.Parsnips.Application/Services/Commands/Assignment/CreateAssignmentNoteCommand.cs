using Ikiru.Parsnips.Domain.Notes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ikiru.Parsnips.Application.Services.Commands.Assignment
{
    public class CreateAssignmentNoteCommand : ICommand
    {
        public Guid SearchFirmId { get; set; }
        public Guid createdByUserId { get; set; }
        public int MyProperty { get; set; }
        public string Title { get; set; }
        public string Text { get; set; }
        public NoteTypeEnum Type { get; set; }
        public ContactMethodEnum ContactMethod { get; set; }
        public bool Pinned { get; set; }
    }
}
