using FluentValidation;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.DomainInfrastructure;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Users.Invite
{
    public class Get
    {
        public class Query : IRequest<Result>
        {
            public string Token { get; set; }
        }

        public class Result
        { 
            public Guid Id { get; set; }
            public Guid SearchFirmId { get; set; }
            public string CompanyName { get; set; }
            public string InviteEmailAddress { get; set; }
        }

        public class QueryValidator : AbstractValidator<Query>
        {
            public QueryValidator()
            {
                RuleFor(q => q.Token)
                   .NotEmpty();
            }
        }

        public class Handler : IRequestHandler<Query, Result>
        {
            private readonly DataQuery m_DataQuery;

            public Handler(DataQuery dataQuery)
            {
                m_DataQuery = dataQuery;
            }

            public async Task<Result> Handle(Query request, CancellationToken cancellationToken)
            {
                var inputTokenValues = request.Token.Split('|');

                var validationErrors = new List<ValidationError>();

                if (inputTokenValues.Length != 2)
                    ThrowInvalidLinkError();

                if (!Guid.TryParse(inputTokenValues[1], out var searchFirmId))
                    ThrowInvalidLinkError();

                if (!Guid.TryParse(inputTokenValues[0], out var token))
                    ThrowInvalidLinkError();

                var feedIteratorSearchFirmUser = m_DataQuery.GetFeedIteratorForDiscriminatedType<SearchFirmUser>(searchFirmId.ToString(),
                                                                                                                 q => q.Where(u => u.InviteToken == token),
                                                                                                                 1);

                var validInvite = await feedIteratorSearchFirmUser.ReadNextAsync(cancellationToken);

                Console.WriteLine($"SearchFirmUser RU's : {validInvite.RequestCharge}");

                if (!validInvite.Any())
                {
                    ThrowInvalidLinkError();
                }

                var searchFirmUser = validInvite.Single();

                if (searchFirmUser.Status == Domain.Enums.SearchFirmUserStatus.Complete)
                    AddError(validationErrors, "This invite link has already been used.", "If you don't have an account ask a member of your team to invite you.");

                var feedIteratorSearchFirm = m_DataQuery.GetFeedIteratorForDiscriminatedType<SearchFirm>(searchFirmId.ToString(), 
                                                                                                         q => q.Where(s => s.SearchFirmId == searchFirmId),
                                                                                                         1);

                var inviteFromSearchFirm = await feedIteratorSearchFirm.ReadNextAsync(cancellationToken);

                Console.WriteLine($"SearchFirm RU's : {inviteFromSearchFirm.RequestCharge}");

                if (!inviteFromSearchFirm.Any())
                    validationErrors.Add(new ValidationError("SearchFirm", "No valid searchfirm found"));
                  
                if (validationErrors.Any())
                    throw new ParamValidationFailureException(validationErrors);

                var searchFirm = inviteFromSearchFirm.Single();

                return new Result
                {
                    Id = searchFirmUser.Id,
                    SearchFirmId = searchFirmUser.SearchFirmId,
                    CompanyName = searchFirm.Name,
                    InviteEmailAddress = searchFirmUser.EmailAddress
                };
            }

            private static void ThrowInvalidLinkError()
            {
                var validationErrors = new List<ValidationError>();
                AddError(validationErrors, "Invalid invite link", "Ask a member of your team to resend your invite link.");

                throw new ParamValidationFailureException(validationErrors);
            }

            private static void AddError(List<ValidationError> validationErrors, string header, string content)
            {
                validationErrors.Add(new ValidationError("header", header));
                validationErrors.Add(new ValidationError("content", content));
            }
        }
    }
}
