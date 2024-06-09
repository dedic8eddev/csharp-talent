using Ikiru.Parsnips.Api.Authentication;
using Ikiru.Parsnips.Api.Controllers.Assignments.Notes.Models;
using Ikiru.Parsnips.Api.Security;
using Ikiru.Parsnips.Application.Services.Assignment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Assignments.Notes
{
    [Route("api/assignments/{id}/notes")]
    [Authorize(Policy = TeamMemberRequirement.POLICY)]
    [ApiController]
    public class AssignmentsNotesController : ControllerBase
    {
        private readonly AuthenticatedUserAccessor _authenticatedUserAccessor;
        private readonly IAssignmentService _assignmentService;

        public AssignmentsNotesController(IAssignmentService assignmentService,
                                        AuthenticatedUserAccessor authenticatedUserAccessor)
        {
            _authenticatedUserAccessor = authenticatedUserAccessor;
            _assignmentService = assignmentService;
        }

        [HttpPost]
        [Consumes("application/json")]
        public async Task<IActionResult> Post(Guid id, CreateNoteModel createNoteModel)
        {
            var user = _authenticatedUserAccessor.GetAuthenticatedUser();

            var response = await _assignmentService.CreateAssignmentNote(assignmentId: id,
                                                            title: createNoteModel.Title,
                                                            createdBy: user.UserId,
                                                            createdDate: createNoteModel.CreatedDate,
                                                            description: createNoteModel.Description,
                                                            searchFirmId: user.SearchFirmId);

            if (response.ValidationErrors.Any())
            {

                return BadRequest(response.ValidationErrors);
            }
            else
            {
                return Ok(response.Value);
            }
        }


        [HttpPut("{noteId}")]
        [Consumes("application/json")]
        public async Task<IActionResult> Put(Guid noteId, Guid id, UpdateNoteModel updateNoteModel)
        {
            var user = _authenticatedUserAccessor.GetAuthenticatedUser();

            var response = await _assignmentService.UpdateAssignmentNote(assignmentId: id,
                                                            title: updateNoteModel.Title,
                                                            updatedBy: user.UserId,
                                                            description: updateNoteModel.Description,
                                                            personId: updateNoteModel.PersonId,
                                                            searchFirmId: user.SearchFirmId,
                                                            noteId: noteId);

            if (response.ValidationErrors.Any())
            {

                return BadRequest(response.ValidationErrors);
            }
            else
            {
                return Ok(response.Value);
            }
        }


        [HttpGet]
        public async Task<IActionResult> Get(Guid id)
        {
            var user = _authenticatedUserAccessor.GetAuthenticatedUser();
            var response = await _assignmentService.GetAllNotesForAssignment(searchFirmId: user.SearchFirmId, assignmentId: id);

            if (response.ValidationErrors.Any())
            {

                return BadRequest(response.ValidationErrors);
            }
            else
            {
                return Ok(response.Value);
            }
        }
    }
}
