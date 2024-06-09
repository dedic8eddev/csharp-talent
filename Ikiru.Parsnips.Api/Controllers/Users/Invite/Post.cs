using FluentValidation;
using Ikiru.Parsnips.Api.Authentication;
using Ikiru.Parsnips.Api.Development;
using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Application.Services;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure;
using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi;
using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi.Models;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues.Items;
using MediatR;
using Microsoft.Extensions.Options;
using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Users.Invite
{
    public class Post
    {
        public class Command : IRequest<Result>
        {
            public string UserEmailAddress { get; set; }
            public UserRole UserRole { get; set; }
        }

        public class Result : Command
        {
            public Guid Id { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(c => c.UserEmailAddress)
                   .NotEmpty()
                   .MaximumLength(255)
                   .EmailAddress();
            }
        }

        public class Handler : IRequestHandler<Command, Result>
        {
            public const string DEFAULT_PASSWORD = "Pass123$";

            private readonly SearchFirmRepository m_SearchFirmRepository;
            private readonly AuthenticatedUserAccessor m_AuthenticatedUserAccessor;
            private readonly IIdentityAdminApi m_IdentityAdminApi;
            private readonly QueueStorage m_QueueStorage;
            private readonly UserSettings m_UserSettings;
            private readonly SearchFirmService _searchFirmService;


            public Handler(SearchFirmRepository searchFirmRepository,
                           AuthenticatedUserAccessor authenticatedUserAccessor,
                           IIdentityAdminApi identityAdminApi,
                           QueueStorage queueStorage,
                           IOptions<UserSettings> userSettings,
                           SearchFirmService searchFirmService)
            {
                m_SearchFirmRepository = searchFirmRepository;
                m_AuthenticatedUserAccessor = authenticatedUserAccessor;
                m_IdentityAdminApi = identityAdminApi;
                m_QueueStorage = queueStorage;
                m_UserSettings = userSettings.Value;
                _searchFirmService = searchFirmService;
            }

            public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
            {
                var authenticatedUser = m_AuthenticatedUserAccessor.GetAuthenticatedUser();

                var licenseAvailable = await _searchFirmService.ChargebeeUserLicenseAvailable(authenticatedUser.SearchFirmId);

                if (!licenseAvailable)
                    throw new ParamValidationFailureException("Licenses", "All of your licenses have been allocated.");


                var inviteToken = Guid.NewGuid();
                var user = new SearchFirmUser(authenticatedUser.SearchFirmId)
                {
                    EmailAddress = command.UserEmailAddress,
                    InviteToken = inviteToken,
                    Status = SearchFirmUserStatus.Invited,
                    UserRole = command.UserRole,
                    InvitedBy = authenticatedUser.UserId
                };

                var createUserRequest = new CreateUserRequest
                {
                    EmailAddress = user.EmailAddress,
                    Password = DEFAULT_PASSWORD,
                    SearchFirmId = authenticatedUser.SearchFirmId,
                    UserId = user.Id,
                    IsDisabled = !m_UserSettings.EnableInvitedUserOnCreation
                };

                try
                {
                    var createUserResult = await m_IdentityAdminApi.CreateUser(createUserRequest);
                    user.SetIdentityUserId(createUserResult.Id);
                }
                catch (ValidationApiException ex)
                {
                    throw new ParamValidationFailureException(ConvertErrors(ex.Content));
                }

                await m_SearchFirmRepository.AddUser(user);

                if (!m_UserSettings.DoNotScheduleInvitationEmail)
                {
                    var inviteEmail = new ConfirmationEmailQueueItem { SearchFirmId = authenticatedUser.SearchFirmId, SearchFirmUserId = user.Id };
                    await m_QueueStorage.EnqueueAsync(QueueStorage.QueueNames.SearchFirmConfirmationEmailQueue, inviteEmail);
                }

                return new Result
                {
                    Id = user.Id,
                    UserRole = user.UserRole,
                    UserEmailAddress = user.EmailAddress
                };
            }

            private static List<ValidationError> ConvertErrors(ProblemDetails problemDetails)
            {
                return problemDetails.Errors.Select(e => new ValidationError(TranslateParameter(e.Key), e.Value.ToArray()))
                                     .ToList();
            }

            private static string TranslateParameter(string paramName)
            {
                switch (paramName)
                {
                    case nameof(CreateUserRequest.EmailAddress):
                        return nameof(Command.UserEmailAddress);

                    default:
                        return paramName;
                }
            }
        }
    }
}