using Ikiru.Parsnips.Application.Services.Notes.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Application.Services
{
    public interface INoteService
    {
        Task<ServiceResponse<NoteResponse>> CreateNote(Guid assignmentId, Guid personId, Guid createdBy, Guid searchFirmId, string title, string description, DateTimeOffset createdDate);
        Task<ServiceResponse<List<NoteResponse>>> GetNotes(Guid searchFirmId, List<Guid> noteIds);
        Task<ServiceResponse<NoteResponse>> UpdateNote(Guid noteId, Guid personId, Guid updatedBy, Guid searchFirmId, string title, string description, DateTimeOffset updatedDate);
    }
}