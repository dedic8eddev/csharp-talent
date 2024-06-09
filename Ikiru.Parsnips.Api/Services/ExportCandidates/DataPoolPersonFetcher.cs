using Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi;
using Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi.Models.Person;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ResourceNotFound;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Services.ExportCandidates
{
    public class DataPoolPersonFetcher
    {
        private readonly ILogger<DataPoolPersonFetcher> m_Logger;
        private readonly IDataPoolApi m_DataPoolApi;

        public DataPoolPersonFetcher(ILogger<DataPoolPersonFetcher> logger, IDataPoolApi dataPoolApi)
        {
            m_Logger = logger;
            m_DataPoolApi = dataPoolApi;
    }

        public async Task<Person> GetPersonById(string datapoolId, CancellationToken cancellationToken)
        {
            Person dataPoolPerson;

            try
            {
                dataPoolPerson = await m_DataPoolApi.Get(datapoolId, cancellationToken);
            }
            catch (Exception ex)
            {
                m_Logger.LogInformation($"Datapool get failure: {ex.Message}");
                throw new ResourceNotFoundException(nameof(IDataPoolApi.Get));
            }

            return dataPoolPerson;
        }
    }
}
