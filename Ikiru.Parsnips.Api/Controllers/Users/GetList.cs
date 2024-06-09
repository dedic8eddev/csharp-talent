using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Ikiru.Parsnips.Api.Authentication;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.DomainInfrastructure;
using MediatR;

namespace Ikiru.Parsnips.Api.Controllers.Users
{
    public class GetList
    {
        public class Query : IRequest<Result>
        {
        }

        public class Result
        {
            public List<UserDetails> Users { get; set; }

            public class UserBaseDetails
            {
                public string FirstName { get; set; }
                public string LastName { get; set; }
                public string EmailAddress { get; set; }
                public string JobTitle { get; set; }
            }

            public class UserDetails : UserBaseDetails
            {
                public Guid Id { get; set; }
                public SearchFirmUserStatus Status { get; set; }
                public UserRole UserRole { get; set; }
                public bool ConfirmationEmailSent { get; set; }
                public DateTimeOffset? ConfirmationEmailSentDate { get; set; }
                public bool IsDisabled { get; set; }
                public UserBaseDetails InvitedBy { get; set; }
            }
        }

        public class Handler : IRequestHandler<Query, Result>
        {
            private readonly AuthenticatedUserAccessor m_AuthenticatedUserAccessor;
            private readonly DataQuery m_DataQuery;
            private readonly IMapper m_Mapper;

            public Handler(AuthenticatedUserAccessor authenticatedUserAccessor, DataQuery dataQuery, IMapper mapper)
            {
                m_AuthenticatedUserAccessor = authenticatedUserAccessor;
                m_DataQuery = dataQuery;
                m_Mapper = mapper;
            }

            public async Task<Result> Handle(Query query, CancellationToken cancellationToken)
            {
                var authenticatedUser = m_AuthenticatedUserAccessor.GetAuthenticatedUser();

                var usersDto = await m_DataQuery.FetchAllItemsForDiscriminatedType<SearchFirmUser>
                                   (authenticatedUser.SearchFirmId.ToString(),
                                    q => q,
                                    cancellationToken);

                var users = new List<Result.UserDetails>();

                foreach (var userDto in usersDto)
                {
                    var user = m_Mapper.Map<Result.UserDetails>(userDto);
                    var invited = usersDto.SingleOrDefault(u => u.Id == userDto.InvitedBy);
                    user.InvitedBy = m_Mapper.Map<Result.UserBaseDetails>(invited);

                    users.Add(user);
                }

                users = users
                       .OrderByDescending(u => u.Id == authenticatedUser.UserId)
                       .ThenBy(u => u.LastName)
                       .ThenBy(u => u.FirstName)
                       .ToList();

                return new Result { Users = users };
            }
        }
    }
}
