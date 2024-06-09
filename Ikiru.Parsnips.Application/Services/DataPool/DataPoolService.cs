using Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi;
using DataPoolModels = Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi.Models.Person;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ResourceNotFound;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Ikiru.Parsnips.Application.Services.DataPool
{
    public class DataPoolService : IDataPoolService
    {
        private readonly IDataPoolApi m_DataPoolApi;
        private readonly ILogger<DataPoolService> m_Logger;

        public DataPoolService(IDataPoolApi dataPoolApi, ILogger<DataPoolService> logger)
        {
            m_DataPoolApi = dataPoolApi;
            m_Logger = logger;
        }

        public async Task<DataPoolModels.Person> GetSinglePersonByWebsiteUrl(string website, CancellationToken cancellationToken)
        {
            var persons = await GetByWebsiteUrl(website, cancellationToken);

            return persons != null && persons.Any()
              ? persons.First()
              : null;
        }

        public async Task<DataPoolModels.Person> GetSinglePersonById(string datapoolId, CancellationToken cancellationToken)
        {
            return await GetPersonById(datapoolId, cancellationToken);
        }


        public async Task<IEnumerable<DataPoolModels.Person>> GetMultiplePersonByWebsiteUrl(string website, CancellationToken cancellationToken)
        {
            var persons = await GetByWebsiteUrl(website, cancellationToken);

            return persons != null && persons.Any()
              ? persons
              : null;
        }

        public async Task<string> GetTempAccessPhotoUrl(Guid queryPersonId, CancellationToken cancellationToken)
        {
            string photoUrl;

            try
            {
                var result = await m_DataPoolApi.GetPersonPhotoUrl(queryPersonId, cancellationToken);
                photoUrl = result?.Photo?.Url ?? "";
            }
            catch (Exception ex)
            {
                if (ex.GetType() != typeof(Refit.ValidationApiException))
                {
                    m_Logger.LogDebug(ex, "Exception calling data pool GetTempAccessPhotoUrl");
                }
                return null;
            }

            return photoUrl;
        }

        private async Task<IEnumerable<DataPoolModels.Person>> GetByWebsiteUrl(string website, CancellationToken cancellationToken)
        {
            IEnumerable<DataPoolModels.Person> dataPoolPersons;

            try
            {
                var websiteEncoded = HttpUtility.UrlEncode(website);
                dataPoolPersons = await m_DataPoolApi.GetByWebsiteUrl(websiteEncoded, cancellationToken);
            }
            catch (Exception ex)
            {
                if (ex.GetType() != typeof(Refit.ValidationApiException))
                {
                    m_Logger.LogDebug(ex, "Exception calling data pool GetByWebsiteUrl");
                }
                return null;
            }

            return dataPoolPersons;
        }

        private async Task<DataPoolModels.Person> GetPersonById(string datapoolId, CancellationToken cancellationToken)
        {
            DataPoolModels.Person dataPoolPerson;

            try
            {
                dataPoolPerson = await m_DataPoolApi.Get(datapoolId, cancellationToken);
            }
            catch (Exception ex)
            {
                m_Logger.LogError($"Datapool error : {ex.Message}");
                throw new ResourceNotFoundException(nameof(IDataPoolApi.Get));
            }

            return dataPoolPerson;
        }

        public async Task<DataPoolModels.Person> PersonScraped(JObject scrapedData, CancellationToken cancellationToken)
        {
            try
            {
                return await m_DataPoolApi.PersonScraped(scrapedData, cancellationToken);
            }
            catch (Exception ex)
            {
                m_Logger.LogDebug(ex, "Exception calling data pool GetByWebsiteUrl");
                throw new ResourceNotFoundException(nameof(IDataPoolApi.PersonScraped));
            }
        }

        public async Task<IEnumerable<DataPoolModels.Person>> GetSimilarPersons(Guid dataPoolPersonId, int pageSize, bool exactSearch, CancellationToken cancellationToken)
        {
            IEnumerable<DataPoolModels.Person> dataPoolPersons;

            try
            {
                dataPoolPersons = await m_DataPoolApi.GetSimilarPersons(dataPoolPersonId, pageSize, exactSearch, cancellationToken);
            }
            catch (Exception ex)
            {
                if (ex.GetType() != typeof(Refit.ValidationApiException))
                {
                    m_Logger.LogDebug(ex, "Exception calling data pool GetSimilarPersonsById");
                }
                return null;
            }

            return dataPoolPersons;
        }

        public async Task<IEnumerable<DataPoolModels.Person>> GetSimilarPersons(string searchString, int pageSize, CancellationToken cancellationToken)
        {
            IEnumerable<DataPoolModels.Person> dataPoolPersons;

            try
            {
                dataPoolPersons = await m_DataPoolApi.GetSimilarPersons(searchString, pageSize, cancellationToken);
            }
            catch (Exception ex)
            {
                if (ex.GetType() != typeof(Refit.ValidationApiException))
                {
                    m_Logger.LogDebug(ex, "Exception calling data pool GetSimilarPersonsById");
                }
                return null;
            }

            return dataPoolPersons;
        }
    }
}
