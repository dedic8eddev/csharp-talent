using Ikiru.Parsnips.Domain.Notes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Application.Services.Person
{
    public interface IPersonNoteService
    {
        Task<PersonNote> CreateNote(Guid searchFirmId, Guid createdBy, Guid personId, Guid assignmentId,
                                            string title, string text, NoteTypeEnum type,
                                            ContactMethodEnum contactMethod);
        Task<PersonNote> GetNoteById(Guid noteId);

        Task<PersonNote> UpdateNote(Guid id, Guid personId, Guid assignmentId,
                                            string title, string text, NoteTypeEnum type,
                                            ContactMethodEnum contactMethod, DateTimeOffset lastEdited, Guid lastEditedBy);

    }
}
