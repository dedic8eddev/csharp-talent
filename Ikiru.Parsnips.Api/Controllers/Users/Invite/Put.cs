using AutoMapper;
using FluentValidation;
using Ikiru.Parsnips.Api.Validators;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.DomainInfrastructure;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ResourceNotFound;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure;
using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi;
using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi.Models;
using MediatR;
using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Users.Invite
{
    public class Put
    {
        public class Command : IRequest
        {
            public Guid Id { get; set; }
            public Guid? SearchFirmId { get; set; } // Because endpoint is non-auth
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string JobTitle { get; set; }
            public string Password { get; set; }
            public string EmailAddress { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(c => c.SearchFirmId)
                   .NotNull()
                   .NotEmptyGuid();

                RuleFor(c => c.FirstName)
                   .NotEmpty();

                RuleFor(c => c.LastName)
                   .NotEmpty();

                RuleFor(c => c.Password)
                   .NotEmpty()
                   .MinimumLength(8);

                RuleFor(c => c.EmailAddress)
                   .MaximumLength(255);
            }
        }

        public class Handler : AsyncRequestHandler<Command>
        {
            private readonly DataStore m_DataStore;
            private readonly IIdentityAdminApi m_IdentityAdminApi;
            private readonly IMapper m_Mapper;

            public Handler(DataStore dataStore, IIdentityAdminApi identityAdminApi, IMapper mapper)
            {
                m_DataStore = dataStore;
                m_IdentityAdminApi = identityAdminApi;
                m_Mapper = mapper;
            }

            protected override async Task Handle(Command request, CancellationToken cancellationToken)
            {
                var currentSearchFirmUser = await m_DataStore.Fetch<SearchFirmUser>(request.Id, request.SearchFirmId.Value);

                if (currentSearchFirmUser == null)
                    throw new ResourceNotFoundException("Invite", request.Id.ToString());

                if (currentSearchFirmUser.EmailAddress != request.EmailAddress)
                    throw new ParamValidationFailureException(nameof(Command.EmailAddress), "Supplied email and email registered with invite does not match");
                
                m_Mapper.Map(request, currentSearchFirmUser);
                currentSearchFirmUser.Status = Domain.Enums.SearchFirmUserStatus.Complete;
                
                var updateUserRequest = new UpdateUserRequest
                {
                    Password = request.Password,
                    EmailConfirmed = true
                };

                try
                {
                    await m_IdentityAdminApi.UpdateUser(currentSearchFirmUser.IdentityUserId, updateUserRequest);
                }
                catch (ValidationApiException ex)
                {
                    throw new ParamValidationFailureException(ConvertErrors(ex.Content));
                }

                await m_DataStore.Update(currentSearchFirmUser, cancellationToken);
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
                    case nameof(CreateUserRequest.UserId):
                        return nameof(Command.EmailAddress);

                    default:
                        return paramName;
                }
            }
        }
    }
}
