using Ikiru.Parsnips.Api.Services.SearchFirmAccountSubscription;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.DomainInfrastructure;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure;
using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi;
using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.SearchFirms
{
    public class Put
    {
        public class Command : IRequest
        {
            public string InviteToken { get; set; }
        }

        public class Handler : AsyncRequestHandler<Command>
        {
            private readonly DataQuery m_DataQuery;
            private readonly DataStore m_DataStore;
            private readonly ISubscribeToTrialService m_SubscribeToTrialService;
            private readonly IIdentityAdminApi m_IdentityAdminApi;

            public Handler(DataQuery dataQuery, DataStore dataStore, ISubscribeToTrialService subscribeToTrialService, IIdentityAdminApi identityAdminApi)
            {
                m_DataQuery = dataQuery;
                m_DataStore = dataStore;
                m_SubscribeToTrialService = subscribeToTrialService;
                m_IdentityAdminApi = identityAdminApi;
            }

            protected override async Task Handle(Command command, CancellationToken cancellationToken)
            {
                var inputTokenValues = command.InviteToken.Split('|');
                var validationErrors = new List<ValidationError>();

                if (inputTokenValues.Length != 2)
                    throw new ParamValidationFailureException(nameof(command.InviteToken), "{Param} is invalid");

                if (!Guid.TryParse(inputTokenValues[1], out var searchFirmId))
                    validationErrors.Add(new ValidationError(nameof(command.InviteToken), "{Param} is invalid"));

                if (!Guid.TryParse(inputTokenValues[0], out var token))
                    validationErrors.Add(new ValidationError(nameof(command.InviteToken), "{Param} is invalid"));

                if (validationErrors.Any())
                    throw new ParamValidationFailureException(validationErrors);
                
                var searchFirmUserIterator = m_DataQuery.GetFeedIteratorForDiscriminatedType<SearchFirmUser>(searchFirmId.ToString(),
                                                                                           x => x.Where(a => a.InviteToken == token), 1);
               
                var searchFirmUserResponse = await searchFirmUserIterator.ReadNextAsync(cancellationToken);
                
                if (!searchFirmUserResponse.Any())
                    throw new ParamValidationFailureException(nameof(Command.InviteToken), "{Param} did not match an Invitation.");
                
                var searchFirmUser = searchFirmUserResponse.Single();

                if (searchFirmUser.Status == SearchFirmUserStatus.Complete)
                    throw new ParamValidationFailureException(nameof(Command.InviteToken), "This registration has already been completed.");

                searchFirmUser.Status = SearchFirmUserStatus.Complete;

                await m_IdentityAdminApi.UpdateUser(searchFirmUser.IdentityUserId, new UpdateUserRequest
                                                                  {
                                                                      EmailConfirmed = true
                                                                  });


                await m_DataStore.Update(searchFirmUser, cancellationToken);

                await m_SubscribeToTrialService.SubscribeToTrial(new SearchFirmAccountTrialSubscriptionModel
                                                               {
                                                                   SearchFirmId = searchFirmUser.SearchFirmId,
                                                                   MainEmail = searchFirmUser.EmailAddress,
                                                                   CustomerFirstName = searchFirmUser.FirstName,
                                                                   CustomerLastName = searchFirmUser.LastName
                                                               });
            }
        }
    }
}
