using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Application.Infrastructure.Storage
{
    public interface IStorageInfrastructure
    {
        Task<Uri> GetTemporaryUrl(Guid searchFirmid, Guid personId, CancellationToken cancellationToken);
        Task<string> GetBlobUri(Guid searchFirmid, Guid personId, CancellationToken cancellationToken);
    }
}
