using Ikiru.Parsnips.Application.Services.Assignment.Models;
using Ikiru.Parsnips.Application.Services.Notes.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Application.Services.Assignment
{
    public interface IAssignmentService : IAssignmentNoteService
    {
        Task<ServiceResponse<NoteResponse>> CreateAssignmentNote(Guid assignmentId, Guid createdBy, Guid searchFirmId, string title, string description, DateTimeOffset createdDate);
        Task<ServiceResponse<List<NoteResponse>>> GetAllNotesForAssignment(Guid searchFirmId, Guid assignmentId);
        Task<ServiceResponse<ActiveAssignmentSimpleResponse>> GetSimple(Guid searchFirmId, int? totalItemCount);
        Task<ServiceResponse<NoteResponse>> UpdateAssignmentNote(Guid assignmentId, Guid noteId, Guid personId, Guid updatedBy, Guid searchFirmId, string title, string description);
        Task<ShareAssignmentResultModel> Share(ShareAssignmentCommand command);
        Task<GetSharedResultModel> GetShared(GetSharedAssignmentCommand command);
        Task<ListSharedAssignmentDetailsModel> GetSharedAssignmentsForClient(Guid searchFirmId, Guid identityServerId);
        Task<GetSharedAssignmentResult> GetSharedAssignmentForPortalUser(GetSharedAssignmentDetailsCommand command);
        Task Delete(UnshareAssignmentCommand command);
    }
}