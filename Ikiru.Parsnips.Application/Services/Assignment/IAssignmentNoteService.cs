using Ikiru.Parsnips.Domain.Notes;
using System;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Application.Services.Assignment
{
    public interface IAssignmentNoteService
    {
        Task<AssignmentNote> CreateNote(Guid searchFirmId,
                        Guid createdByUserId,
                        Guid assignmentId,
                        string title,
                        string text,
                        NoteTypeEnum type,
                        ContactMethodEnum contactMethod);

        Task<AssignmentNote> UpdateNote(Guid id, string title,
                                                string text, NoteTypeEnum type, ContactMethodEnum contactMethod,
                                                DateTimeOffset lastEdited, Guid lastEditedBy, bool pinned);

        Task<AssignmentNote> GetNoteById(Guid noteId);
    }
}
