using Ikiru.Parsnips.Application.Services.Person.Models;
using Ikiru.Parsnips.Domain.Notes;
using System;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Application.Services.Person
{
    public interface IPersonService
    {
        Task<SearchPersonQueryResult> SearchPersonByQuery(Models.SearchPersonQueryRequest query);
        Task<PersonNote> CreateNote(Guid searchFirmId, Guid createdBy, Guid personId, Guid assignmentId, string title, string text, NoteTypeEnum type, ContactMethodEnum contactMethod);
        Task<GetByWebsiteUrlResponse> GetByWebSiteUrl(GetByWebsiteUrlRequest query);
        Task<GetLocalPersonByWebsiteUrlResponse> GetLocalPersonResult(GetByWebsiteUrlRequest query);
        Task<PersonNote> GetNoteById(Guid noteId);
        Task<PersonNote> UpdateNote(Guid id, Guid personId, Guid assignmentId, string title, string text, NoteTypeEnum type, ContactMethodEnum contactMethod, DateTimeOffset lastEdited, Guid lastEditedBy);
    }
}