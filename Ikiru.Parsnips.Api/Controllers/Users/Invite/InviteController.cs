using Ikiru.Parsnips.Api.Authentication;
using Ikiru.Parsnips.Api.Controllers.Users.Invite.Models;
using Ikiru.Parsnips.Api.Security;
using Ikiru.Parsnips.Application.Query.Users;
using Ikiru.Parsnips.Application.Services;
using Ikiru.Parsnips.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Users.Invite
{
    [AllowInactiveSubscriptions]
    [ApiController]
    [Route("/api/users/[controller]")]
    public class InviteController : ControllerBase
    {
        private readonly IMediator m_Mediator;
        private readonly AuthenticatedUserAccessor _authenticatedUserAccessor;
        private readonly InviteProcessor _inviteProcessor;
        private readonly SearchFirmService _searchFirmService;

        public InviteController(IMediator mediator, AuthenticatedUserAccessor authenticatedUserAccessor, 
                                InviteProcessor inviteProcessor, SearchFirmService searchFirmService)
        {
            m_Mediator = mediator;
            _authenticatedUserAccessor = authenticatedUserAccessor;
            _inviteProcessor = inviteProcessor;
            _searchFirmService = searchFirmService;
        }

        [Authorize(Policy = AdminRequirement.POLICY)]
        [HttpPost]
        [Consumes("application/json")]
        public async Task<IActionResult> Post([FromBody] Post.Command command)
        {
            var result = await m_Mediator.Send(command);
            return Ok(result);

        }


        [Authorize(Policy = AdminRequirement.POLICY)]
        [HttpPost("[action]")]
        [Consumes("application/json")]
        public async Task<IActionResult> InviteMultiple(MultipleInvitesModel[] multipleInvitesModel)
        {
            var invitesAreValid = await _searchFirmService.InviteMultipleUsersAreValid(multipleInvitesModel.Select(x => x.Email).ToArray());

            if (!invitesAreValid)
            {
                return BadRequest();
            }

            foreach (var userInvite in multipleInvitesModel)
            {
                var command = new Post.Command
                {
                    UserEmailAddress = userInvite.Email,
                    UserRole = userInvite.UserRole
                };

                await m_Mediator.Send(command);
            }

            return NoContent();
        }

        [Authorize(Policy = AdminRequirement.POLICY)]
        [HttpPost("/api/users/{id}/[controller]/[action]")]
        [Consumes("application/json")]
        public async Task<IActionResult> Resend(Guid id)
        {
            var searchFirmId = _authenticatedUserAccessor.GetAuthenticatedUser().SearchFirmId;
            await _inviteProcessor.ResendToUser(searchFirmId, id);
            return NoContent();

        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Get([FromQuery]Get.Query query)
        {
            var result = await m_Mediator.Send(query);
            return Ok(result);           
        }

        [HttpPut("{id}")]
        [AllowAnonymous]
        [Consumes("application/json")]
        public async Task<IActionResult> Put(Guid id, [FromBody]Put.Command command)
        {
            command.Id = id;
            await m_Mediator.Send(command);
            return NoContent();
        }

        [Authorize(Policy = AdminRequirement.POLICY)]
        [HttpPut("/api/users/{id}/[controller]/[action]")]
        [Consumes("application/json")]
        public async Task<IActionResult> Revoke(Guid id)
        {
            var searchFirmId = _authenticatedUserAccessor.GetAuthenticatedUser().SearchFirmId;
            await _inviteProcessor.Revoke(searchFirmId, id);
            return NoContent();
        }
    }
}
