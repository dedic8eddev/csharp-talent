using Ikiru.Parsnips.Domain;
using Ikiru.Persistence.Repository;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Application.Persistence
{
    public class PortalUserRepository : RepositoryBase<PortalUser>
    {
        private readonly IRepository _repository;

        public PortalUserRepository(IRepository repository) : base(repository)
        {
            _repository = repository;
        }

        public async Task<PortalUser> GetUser(Guid searchFirmId, Guid idServerId)
        {
            var result = await _repository.GetByQuery<PortalUser, PortalUser>(searchFirmId.ToString(),
                        users => users.Where(u => u.IdentityServerId == idServerId));

            return result.SingleOrDefault();
        }

        public async Task<PortalUser> GetUser(Guid searchFirmId, string email)
        {
            var portalUsers = await _repository.GetByQuery<PortalUser, PortalUser>(searchFirmId.ToString(),
                        users => users.Where(u => u.Email == email && u.SearchFirmId == searchFirmId));

            return portalUsers.SingleOrDefault();
        }

        public Task<PortalUser> Upsert(PortalUser portalUser) => _repository.UpdateItem(portalUser);

        public async Task<PortalUser> GetUserByIdentityId(Guid searchFirmId, Guid identityServerId)
        {
            var portalUsers = await _repository.GetByQuery<PortalUser, PortalUser>(searchFirmId.ToString(),
                item => item.Where(pu => pu.SearchFirmId == searchFirmId && pu.IdentityServerId == identityServerId));

            return portalUsers.SingleOrDefault();
        }
    }
}