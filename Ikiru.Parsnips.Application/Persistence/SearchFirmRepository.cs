using Ikiru.Parsnips.Domain;
using Ikiru.Persistence.Repository;
using Ikiru.Parsnips.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Application.Persistence
{
    public class SearchFirmRepository
    {
        private readonly IRepository _repository;
        public SearchFirmRepository(IRepository persistenceService)
        {
            _repository = persistenceService;
        }

        public Task<SearchFirm> GetSearchFirmById(Guid searchFirmId)
            => _repository.GetItem<SearchFirm>(searchFirmId.ToString(), searchFirmId.ToString());

        public Task<SearchFirmUser> GetUserById(Guid searchFirmId, Guid userId)
            => _repository.GetItem<SearchFirmUser>(searchFirmId.ToString(), userId.ToString());

        public Task<int> GetEnabledUsersNumber(Guid searchFirmId)
            => _repository.Count<SearchFirmUser>(s => s.Discriminator == SearchFirmUser.DiscriminatorName
                                                                            && s.SearchFirmId == searchFirmId
                                                                            && s.IsEnabled == true);

        public Task UpdateSearchFirm(SearchFirm searchFirm) => _repository.UpdateItem(searchFirm);
        public Task UpdateSearchFirmUser(SearchFirmUser searchFirmUser) => _repository.UpdateItem(searchFirmUser);
        public Task AddUser(SearchFirmUser searchFirmUser) => _repository.Add(searchFirmUser);

        public Task<bool> DeleteUser(SearchFirmUser searchFirmUser) => _repository.Delete(searchFirmUser);

        public Task<List<SearchFirmUser>> GetAllUsersByUserRoleForSearchFirm(Guid searchFirmId, UserRole userRole)
        {
            return _repository.GetByQuery<SearchFirmUser>(s => s.SearchFirmId == searchFirmId && s.UserRole == userRole);
        }
    }
}
