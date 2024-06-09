using AutoMapper;
using Ikiru.Parsnips.Application.Persistence;
using System;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Application.Services.PortalUser
{
    public class PortalUserService
    {
        private readonly PortalUserRepository _portalUserRepository;
        private readonly SearchFirmRepository _searchFirmRepository;
        private readonly IMapper _mapper;

        public PortalUserService(PortalUserRepository portalUserRepository, SearchFirmRepository searchFirmRepository, IMapper mapper)
        {
            _portalUserRepository = portalUserRepository;
            _searchFirmRepository = searchFirmRepository;
            _mapper = mapper;
        }

        public async Task<Model.PortalUser> GetPortalUser(Guid searchFirmId, Guid identityServerId)
        {
            var user = await _portalUserRepository.GetUser(searchFirmId, identityServerId);
            var result = _mapper.Map<Model.PortalUser>(user);

            if (result == null)
            {
                return null;
            }

            var searchFirm = await _searchFirmRepository.GetSearchFirmById(searchFirmId);

            result.SearchFirmName = searchFirm?.Name;

            return result;
        }
    }
}
