using Ikiru.Parsnips.Api.Security;
using Ikiru.Parsnips.Api.Services.ExportCandidates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Assignments.ExportCandidates
{
    [ApiController]
    [Authorize(Policy = TeamMemberRequirement.POLICY)]
    [Route("/api/assignments/{assignmentId}/[controller]")]
    public class ExportCandidatesController : ControllerBase
    {
        private readonly ExportCandidatesService m_ExportCandidatesService;

        public ExportCandidatesController(ExportCandidatesService exportCandidatesService)
            => m_ExportCandidatesService = exportCandidatesService;

        [HttpGet]
        public async Task<IActionResult> Get([FromRoute] Guid assignmentId)
        {
            var result = await m_ExportCandidatesService.Generate(assignmentId, CancellationToken.None);

            return File(result.ExportData, "application/octet-stream", result.FileName);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromRoute] Guid assignmentId, Guid[] candidateIds)
        {
            var result = await m_ExportCandidatesService.Generate(assignmentId, CancellationToken.None, candidateIds);

            return File(result.ExportData, "application/octet-stream", result.FileName);
        }
    }
}
