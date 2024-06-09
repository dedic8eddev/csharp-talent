using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DataPoolModels = Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi.Models.Person;

namespace Ikiru.Parsnips.Application.Services.DataPool
{
    public interface IDataPoolService
    {
        Task<DataPoolModels.Person> GetSinglePersonByWebsiteUrl(string website, CancellationToken cancellationToken);

        Task<DataPoolModels.Person> GetSinglePersonById(string datapoolId, CancellationToken cancellationToken);
        Task<string> GetTempAccessPhotoUrl(Guid queryPersonId, CancellationToken cancellationToken);

        Task<IEnumerable<DataPoolModels.Person>> GetSimilarPersons(Guid dataPoolPersonId, int pageSize, bool exactSearch, CancellationToken cancellationToken);
        Task<IEnumerable<DataPoolModels.Person>> GetSimilarPersons(string searchString, int pageSize, CancellationToken cancellationToken);
    }
}