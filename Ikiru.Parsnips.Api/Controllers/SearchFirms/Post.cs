using AutoMapper;
using FluentValidation;
using Ikiru.Parsnips.Api.Development;
using Ikiru.Parsnips.Api.Filters.ValidationFailure;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.DomainInfrastructure;
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
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure;

namespace Ikiru.Parsnips.Api.Controllers.SearchFirms
{
    public class Post
    {
        public class Command : IRequest<Result>
        {
            public string SearchFirmName { get; set; }
            public string SearchFirmCountryCode { get; set; }
            public string SearchFirmPhoneNumber { get; set; }
            public string UserFirstName { get; set; }
            public string UserLastName { get; set; }
            public string UserEmailAddress { get; set; }
            public string UserJobTitle { get; set; }
            public string UserPassword { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(c => c.SearchFirmName)
                   .NotEmpty()
                   .MaximumLength(111);

                RuleFor(c => c.SearchFirmCountryCode)
                   .NotEmpty()
                   .Length(2);

                RuleFor(c => c.SearchFirmPhoneNumber)
                   .MaximumLength(27);

                RuleFor(c => c.UserFirstName)
                   .NotEmpty()
                   .MaximumLength(55);

                RuleFor(c => c.UserLastName)
                   .NotEmpty()
                   .MaximumLength(55);

                RuleFor(c => c.UserEmailAddress)
                   .NotEmpty()
                   .MaximumLength(255)
                   .EmailAddress();

                RuleFor(c => c.UserJobTitle)
                   .MaximumLength(121);

                RuleFor(c => c.UserPassword)
                   .NotEmpty()
                   .Length(8, 20);
            }
        }

        public class Result
        {
            public Guid Id { get; set; }
        }

        public enum UserType
        {
            Other,
            InitialUser,
            InvitedUser
        }

        public class UserStatus
        {
            public SearchFirmUserStatus Status { get; set; }
        }

        public class Handler : IRequestHandler<Command, Result>
        {
            private readonly DataStore m_DataStore;
            private readonly DataQuery m_DataQuery;
            private readonly IMapper m_Mapper;
            private readonly IIdentityAdminApi m_IdentityAdminApi;
            private readonly QueueStorage m_QueueStorage;
            private readonly UserSettings m_UserSettings;

            public Handler(DataStore dataStore,
                DataQuery dataQuery,
                IMapper mapper,
                IIdentityAdminApi identityAdminApi,
                QueueStorage queueStorage,
                IOptions<UserSettings> userSettings)
            {
                m_DataStore = dataStore;
                m_DataQuery = dataQuery;
                m_Mapper = mapper;
                m_IdentityAdminApi = identityAdminApi;
                m_QueueStorage = queueStorage;
                m_UserSettings = userSettings.Value;
            }

            public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
            {
                var searchFirm = m_Mapper.Map<SearchFirm>(command);
                searchFirm.IsEnabled = true;

                var user = new SearchFirmUser(searchFirm.Id);
                user = m_Mapper.Map(command, user);

                try
                {
                    var createUserResult = await m_IdentityAdminApi.CreateUser(new CreateUserRequest { EmailAddress = user.EmailAddress, Password = command.UserPassword, SearchFirmId = searchFirm.Id, UserId = user.Id });
                    user.SetIdentityUserId(createUserResult.Id);
                }
                catch (ValidationApiException ex)
                {
                    var validationFailure = ConvertErrors(ex.Content);

                    var identityUserResponse = await m_IdentityAdminApi.GetUser(user.EmailAddress);
                    if (identityUserResponse.IsSuccessStatusCode)
                    {
                        var identityUser = identityUserResponse.Content;

                        Func<IQueryable<SearchFirmUser>, IQueryable<UserStatus>> filter =
                            users => users.Where(user => user.Id == identityUser.UserId).Select(user => new UserStatus { Status = user.Status });

                        var userStatusObject = await m_DataQuery.GetSingleItemForDiscriminatedType(identityUser.SearchFirmId.ToString(), filter);

                        validationFailure.Add(AddMessage("emailConfirmed", identityUser.EmailConfirmed));
                        validationFailure.Add(AddMessage("accountDisabled", identityUser.IsDisabled));
                        validationFailure.Add(AddMessage("userType", userStatusObject.Status));
                    }

                    throw new ParamValidationFailureException(validationFailure);
                }

                // Note: Not transactional
                searchFirm = await m_DataStore.Insert(searchFirm, cancellationToken);

                user.InviteToken = Guid.NewGuid();
                user.Status = SearchFirmUserStatus.InvitedForNewSearchFirm;
                user.UserRole = UserRole.Owner;
                await m_DataStore.Insert(user, cancellationToken);

                if (!m_UserSettings.DoNotScheduleInvitationEmail)
                {
                    var searchFirmConfirmationEmailQueueItem = new ConfirmationEmailQueueItem
                    {
                        SearchFirmId = searchFirm.Id,
                        SearchFirmUserId = user.Id
                    };

                    await m_QueueStorage.EnqueueAsync(QueueStorage.QueueNames.SearchFirmConfirmationEmailQueue,
                                             searchFirmConfirmationEmailQueueItem);
                }
                else if (m_UserSettings.CreateActiveFakeSubscription)
                {
                    await CreateActiveFakeSubscription(user);
                }

                return m_Mapper.Map<Result>(searchFirm);
            }

            private async Task CreateActiveFakeSubscription(SearchFirmUser searchFirmUser)
            {
                Func<IQueryable<ChargebeePlan>, IQueryable<string>> filter =
                    plans => plans.Where(p => p.PlanType == PlanType.Trial).Select(p => p.PlanId);

                var planId = await m_DataQuery.GetFirstOrDefaultItemForDiscriminatedType(ChargebeePlan.PartitionKey, filter);

                var subscription = new ChargebeeSubscription(searchFirmUser.SearchFirmId)
                {
                    MainEmail = searchFirmUser.EmailAddress,
                    PlanId = planId,
                    CustomerId = $"Fake-customer-{searchFirmUser.SearchFirmId}",
                    SubscriptionId = $"Fake-subscription-{searchFirmUser.SearchFirmId}",
                    IsEnabled = true,
                    Status = Domain.Chargebee.Subscription.StatusEnum.Active,
                    CurrentTermEnd = DateTimeOffset.MaxValue,
                    PlanQuantity = int.MaxValue
                };

                await m_DataStore.Insert(subscription, CancellationToken.None);
            }

            private ValidationError AddMessage(string paramName, SearchFirmUserStatus status)
            {
                var type = status == SearchFirmUserStatus.InvitedForNewSearchFirm
                                ? UserType.InitialUser
                                : status == SearchFirmUserStatus.Invited
                                    ? UserType.InvitedUser
                                    : UserType.Other;

                return new ValidationError(paramName, type);
            }

            private ValidationError AddMessage(string paramName, bool isDisabled)
                => new ValidationError(paramName, isDisabled);

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

                    case nameof(CreateUserRequest.Password):
                        return nameof(Command.UserPassword);

                    default:
                        return paramName;
                }
            }
        }
    }
}
