using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Ikiru.Parsnips.Domain.Notes
{
    public class AssignmentNote : Note
    {
        public string Descriminator => nameof(AssignmentNote);
        public Guid AssignmentId { get; private set; }

        [Required]
        [MaxLength(100)]
        public string Title { get; private set; }
        public string Text { get; private set; }
        public NoteTypeEnum Type { get; private set; }
        public ContactMethodEnum ContactMethod { get; private set; }
        public bool Pinned { get; private set; }

        public AssignmentNote(Guid searchFirmId,
                        Guid createdByUserId,
                        Guid assignmentId,
                        string title,
                        string text,
                        NoteTypeEnum type,
                        ContactMethodEnum contactMethod,
                        bool pinned = false) : base(searchFirmId, createdByUserId)
        {
            AssignmentId = assignmentId;
            Title = title;
            Text = text;
            Type = type;
            ContactMethod = contactMethod;
            Pinned = pinned;
        }

        public void Update(string title,
                            string text,
                            NoteTypeEnum type,
                            ContactMethodEnum contactMethod,
                            DateTimeOffset lastEdited,
                            Guid lastEditedBy,
                            bool pinned = false)
        {
            Title = title;
            Text = text;
            Type = type;
            ContactMethod = contactMethod;
            Pinned = pinned;
            LastEdited = lastEdited;
            LastEditedBy = lastEditedBy;
        }
    }
}
