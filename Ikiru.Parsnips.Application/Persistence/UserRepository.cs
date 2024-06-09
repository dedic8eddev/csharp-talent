using Ikiru.Parsnips.Domain;
using Ikiru.Persistence.Repository;
using System;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Application.Persistence
{
    public class UserRepository
    {
        private IRepository _repository;

        public UserRepository(IRepository repository)
        {
            _repository = repository;
        }

        public Task<SearchFirmUser> GetUserById(Guid userId, Guid searchFirmId)
            => _repository.GetItem<SearchFirmUser>(searchFirmId.ToString(), userId.ToString());

        public Task<SearchFirmUser> Update(SearchFirmUser searchFirmUser) 
            => _repository.UpdateItem(searchFirmUser);
    }
}
