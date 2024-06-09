using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Ikiru.Parsnips.Domain.Notes
{
    public class PersonNote : Note
    {
        public string Descriminator => nameof(PersonNote); 
        public Guid? AssignmentId { get; private set; }
        public Guid PersonId { get; private set; }

        [Required]
        [MaxLength(100)]
        public string Title { get; private set; }
        [Required]
        public string Text { get; private set; }
        public NoteTypeEnum Type { get; private set; }
        public ContactMethodEnum ContactMethod { get; private set; }
        public bool Pinned { get; private set; }

        public PersonNote(Guid searchFirmId, 
                         Guid createdByUserId,
                         Guid personId,
                         string title,
                         string text,
                         NoteTypeEnum type,
                         ContactMethodEnum contactMethod,
                         bool pinned = false,
                         Guid? assignmentId = null) : base(searchFirmId, createdByUserId)
        {
            AssignmentId = assignmentId;
            PersonId = personId;
            Title = title;
            Text = text;
            Type = type;
            ContactMethod = contactMethod;
            Pinned = pinned;
        }

        public void Update (string title,
                           string text,
                           NoteTypeEnum type,
                           ContactMethodEnum contactMethod,
                           DateTimeOffset lastEdited,
                           Guid lastEditedBy,
                           bool pinned = false,
                           Guid? assignmentId = null)
        {
            AssignmentId = assignmentId;
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
