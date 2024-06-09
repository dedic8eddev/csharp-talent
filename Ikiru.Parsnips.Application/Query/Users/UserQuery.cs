using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Application.Query.Users.Models;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Application.Query.Users
{
    public class UserQuery : IQueryHandler<GetActiveUsersRequest, GetActiveUsersResponse>
    {
        private readonly SearchFirmRepository _searchFirmRepository;

        public UserQuery(SearchFirmRepository searchFirmRepository)
        {
            _searchFirmRepository = searchFirmRepository;
        }
        public async Task<GetActiveUsersResponse> Handle(GetActiveUsersRequest query)
        {
            var getActiveUsersResponse = new GetActiveUsersResponse();
            getActiveUsersResponse.Count = await _searchFirmRepository.GetEnabledUsersNumber(query.SearchFirmId);

            return getActiveUsersResponse;
        }
    }
}
