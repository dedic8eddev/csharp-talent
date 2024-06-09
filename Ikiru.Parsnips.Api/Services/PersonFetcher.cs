using Ikiru.Parsnips.Api.Authentication;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.DomainInfrastructure;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ResourceNotFound;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Ikiru.Parsnips.Application.Persistence;

namespace Ikiru.Parsnips.Api.Services
{
    public class PersonFetcher
    {
        private readonly PersonRepository _personRepository;
        private readonly DataStore m_DataStore;
        private readonly DataQuery m_DataQuery;
        private readonly AuthenticatedUserAccessor m_AuthenticatedUserAccessor;
        private readonly ILogger<PersonFetcher> m_Logger;

        public PersonFetcher(PersonRepository personRepository, DataStore dataStore, DataQuery dataQuery, AuthenticatedUserAccessor authenticatedUserAccessor, ILogger<PersonFetcher> logger)
        {
            _personRepository = personRepository;
            m_DataStore = dataStore;
            m_DataQuery = dataQuery;
            m_AuthenticatedUserAccessor = authenticatedUserAccessor;
            m_Logger = logger;
        }

        /// <summary>
        /// Gets person by id inside the current search firm. Throws <a>ResourceNotFoundException</a> if not found.
        /// </summary>
        /// <returns>The Person entity for the Id</returns>
        public async Task<Person> GetPersonOrThrow(Guid personId)
        {
            var searchFirmId = m_AuthenticatedUserAccessor.GetAuthenticatedUser().SearchFirmId;
            var person = await _personRepository.GetById(searchFirmId, personId);

            if (person == null)
            {
                m_Logger.LogDebug($"Person with id '{personId}' not found.");
                throw new ResourceNotFoundException(nameof(Person), personId.ToString());
            }

            return person;
        }

        /// <summary>
        /// Fetch Person by Id for data updates. Throws <a>ResourceNotFoundException</a> if not found.
        /// </summary>
        /// <returns>The Person entity for the Id</returns>
        [Obsolete("Use GetPersonOrThrow instead.")]
        public async Task<Domain.Person> FetchPersonOrThrow(Guid personId, CancellationToken cancellationToken)
        {
            var searchFirmId = m_AuthenticatedUserAccessor.GetAuthenticatedUser().SearchFirmId;

            Domain.Person person;
            try
            {
                person = await m_DataStore.Fetch<Domain.Person>(personId, searchFirmId, cancellationToken);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                m_Logger.LogDebug(ex, $"Person '{personId}' not found.");
                throw new ResourceNotFoundException(nameof(Person), personId.ToString());
            }

            m_Logger.LogTrace($"Found person '{person.Id}'.");

            return person;
        }

        /// <summary>
        /// Retrieve the Person by Id for read only purposes. Throws <a>ResourceNotFoundException</a> if no matches by Id.
        /// </summary>
        /// <returns>The Person entity for the Id</returns>
        [Obsolete("Use FetchPersonOrThrow instead.")]
        public async Task<Domain.Person> FindPersonOrThrow(Guid personId, CancellationToken cancellationToken)
        {
            var searchFirmId = m_AuthenticatedUserAccessor.GetAuthenticatedUser().SearchFirmId;

            var person = await m_DataQuery.GetSingleItem<Domain.Person>(searchFirmId.ToString(), i => i.Where(p => p.Id == personId), cancellationToken);

            if (person != null)
                return person;

            m_Logger.LogInformation($"Person '{personId}' not found.");
            throw new ResourceNotFoundException(nameof(Domain.Person), personId.ToString());
        }

        public async Task<Domain.Person> FindPersonByLinkedinProfileId(string linkedinProfileId, CancellationToken cancellationToken)
        {
            var searchFirmId = m_AuthenticatedUserAccessor.GetAuthenticatedUser().SearchFirmId;

            return await m_DataQuery.GetSingleItem<Domain.Person>(searchFirmId.ToString(), i => i.Where(x => x.LinkedInProfileId == linkedinProfileId), cancellationToken);
        }

        public async Task<Domain.Person> FindPersonByWebsiteUrl(string websiteUrl, CancellationToken cancellationToken)
        {
            var searchFirmId = m_AuthenticatedUserAccessor.GetAuthenticatedUser().SearchFirmId;

            var person = await m_DataQuery.GetSingleItem<Domain.Person>(searchFirmId.ToString(), i => i.Where(x => x.WebSites.Any(x => x.Url == websiteUrl)), cancellationToken);

            if (person != null)
                return person;

            m_Logger.LogInformation($"Person '{websiteUrl}' not found.");
            return null;
        }
    }
}
