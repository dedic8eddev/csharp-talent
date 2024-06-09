using AutoMapper;
using Ikiru.Parsnips.Api.Authentication;
using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Users
{
    public class Put
    {
        public class Command : IRequest<Result>
        {
            public Guid Id { get; set; }
            public UserRole UserRole { get; set; }
        }

        public class Result : Command
        {
        }

        public class Handler : IRequestHandler<Command, Result>
        {
            private readonly AuthenticatedUserAccessor _authenticatedUserAccessor;
            private readonly UserRepository _userRepository;
            private readonly IMapper _mapper;

            public Handler(AuthenticatedUserAccessor authenticatedUserAccessor, UserRepository userRepository, IMapper mapper)
            {
                _authenticatedUserAccessor = authenticatedUserAccessor;
                _userRepository = userRepository;
                _mapper = mapper;
            }

            public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
            {
                var authenticatedUser = _authenticatedUserAccessor.GetAuthenticatedUser();
                
                var loggedInUser = await _userRepository.GetUserById(authenticatedUser.UserId, authenticatedUser.SearchFirmId);

                if (loggedInUser.UserRole == UserRole.TeamMember)
                    throw new ParamValidationFailureException(nameof(UserRole), $"User '{loggedInUser.FirstName} {loggedInUser.LastName}' is not allowed to update user roles.");

                var user = await _userRepository.GetUserById(command.Id, authenticatedUser.SearchFirmId);

                if (loggedInUser.UserRole == UserRole.Admin && user.UserRole == UserRole.Owner)
                    throw new ParamValidationFailureException(nameof(UserRole), $"User '{loggedInUser.FirstName} {loggedInUser.LastName}' is not allowed to update user role for '{user.FirstName} {user.LastName}'.");

                if (loggedInUser.UserRole != UserRole.Owner && command.UserRole == UserRole.Owner)
                    throw new ParamValidationFailureException(nameof(UserRole), $"User '{loggedInUser.FirstName} {loggedInUser.LastName}' is not allowed to elevate roles to {UserRole.Owner}.");

                if (loggedInUser.Id == command.Id && loggedInUser.UserRole == UserRole.Owner && command.UserRole != UserRole.Owner)
                    throw new ParamValidationFailureException(nameof(UserRole), $"User '{loggedInUser.FirstName} {loggedInUser.LastName}' is not allowed to change your own role from {UserRole.Owner}.");

                _mapper.Map(command, user);

                user = await _userRepository.Update(user);

                return _mapper.Map<Result>(user);
            }
        }
    }
}
