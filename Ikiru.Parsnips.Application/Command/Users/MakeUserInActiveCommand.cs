using Ikiru.Parsnips.Application.Command.Users.Models;
using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Application.Services;
using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi;
using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi.Models;
using System;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Application.Command.Users
{
    public class MakeUserInactiveCommand : ICommandHandler<MakeUserInActiveRequest, MakeUserInActiveResponse>,
                                             ICommandHandler<MakeUserActiveRequest, MakeUserActiveResponse>
    {
        private readonly SearchFirmRepository _searchFirmRepository;
        private readonly IIdentityAdminApi _identityAdminApi;
        private readonly SearchFirmService _searchFirmService;

        public MakeUserInactiveCommand(SearchFirmRepository searchFirmRepository,
                                        IIdentityAdminApi identityAdminApi,
                                        SearchFirmService searchFirmService)
        {
            _searchFirmRepository = searchFirmRepository;
            _identityAdminApi = identityAdminApi;
            _searchFirmService = searchFirmService;
        }

        public async Task<MakeUserInActiveResponse> Handle(MakeUserInActiveRequest command)
        {
            var makeUserInActiveResponse = new MakeUserInActiveResponse();

            try
            {
                makeUserInActiveResponse.Response = await MakeSearchFirmuserActiveOrInActive(loggedInUserId: command.SearchFirmUserIdLoggedIn,
                                                                                    searchFirmId: command.SearchFirmId,
                                                                                    userIdToChangeStatus: command.SearchFirmUserIdToMakeInActive,
                                                                                    makeUserInActive: true);
            }
            catch
            {
                makeUserInActiveResponse.Response = UserActiveInActiveStatusEnum.InternalError;
            }
            
            return makeUserInActiveResponse;
        }

        public async Task<MakeUserActiveResponse> Handle(MakeUserActiveRequest command)
        {
            var makeUserActiveResponse = new MakeUserActiveResponse();

            try
            {
                makeUserActiveResponse.Response = await MakeSearchFirmuserActiveOrInActive(loggedInUserId: command.SearchFirmUserIdLoggedIn,
                                                                                     searchFirmId: command.SearchFirmId,
                                                                                     userIdToChangeStatus: command.SearchFirmUserIdToMakeActive,
                                                                                     makeUserInActive: false);
            }
            catch
            {
                makeUserActiveResponse.Response = UserActiveInActiveStatusEnum.InternalError;
            }

            return makeUserActiveResponse;
        }

        private async Task<UserActiveInActiveStatusEnum> MakeSearchFirmuserActiveOrInActive(Guid loggedInUserId,
                                                                    Guid searchFirmId,
                                                                    Guid userIdToChangeStatus,
                                                                    bool makeUserInActive)
        {
            var searchFirmUserLoggedIn = await _searchFirmRepository.GetUserById(searchFirmId, loggedInUserId);

            if (!(searchFirmUserLoggedIn.UserRole == Domain.Enums.UserRole.Admin ||
                searchFirmUserLoggedIn.UserRole == Domain.Enums.UserRole.Owner))
            {
                return UserActiveInActiveStatusEnum.IncorrectPermission;
            }

            var searchFirmUserToChangeStatus = await _searchFirmRepository.GetUserById(searchFirmId, userIdToChangeStatus);

            if (searchFirmUserToChangeStatus == null)
            {
                return UserActiveInActiveStatusEnum.UserToChangeStatusDoesNotExist;
            }


            var identityUpdateUserRequest = new UpdateUserRequest();

            if (makeUserInActive)
            {
                if (searchFirmUserLoggedIn.Id == searchFirmUserToChangeStatus.Id)
                {
                    return UserActiveInActiveStatusEnum.UnableToDisableOwnAccount;
                }

                if (searchFirmUserToChangeStatus.UserRole == Domain.Enums.UserRole.Owner)
                
                {
                    var searchFirmUserOwners = await _searchFirmRepository.GetAllUsersByUserRoleForSearchFirm(searchFirmId, Domain.Enums.UserRole.Owner);

                    if (searchFirmUserOwners.Count == 1)
                    {
                        return UserActiveInActiveStatusEnum.UnableToDisableThereMustBeGreaterThenOneOwnerAccount;
                    }
                }

                searchFirmUserToChangeStatus.IsDisabled = true;

                identityUpdateUserRequest.DisableLogin = true;
                identityUpdateUserRequest.DisableLoginEndDate = DateTimeOffset.MaxValue;
            }
            else
            {               
                if(await _searchFirmService.ChargebeeUserLicenseAvailable(searchFirmId))
                {
                    searchFirmUserToChangeStatus.IsDisabled = false;

                    identityUpdateUserRequest.DisableLogin = false;
                    identityUpdateUserRequest.DisableLoginEndDate = DateTimeOffset.Now;
                }
                else
                {
                    return UserActiveInActiveStatusEnum.NoAvailableLicenses;
                }
            }

            try
            {
                await _searchFirmRepository.UpdateSearchFirmUser(searchFirmUserToChangeStatus);

                await _identityAdminApi.UpdateUser(searchFirmUserToChangeStatus.IdentityUserId, identityUpdateUserRequest); 

                if (searchFirmUserToChangeStatus.IsDisabled)
                {
                    return UserActiveInActiveStatusEnum.IsInActive;
                }
                else
                {
                    return UserActiveInActiveStatusEnum.IsActive;
                }
             
            }
            catch (Exception ex)
            {
                throw new Exception($"unable to update status for user: {searchFirmUserToChangeStatus.Id} by loggedin user : {searchFirmUserLoggedIn.Id}", ex);
            }
        }
    }
}
