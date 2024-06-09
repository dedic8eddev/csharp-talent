using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.DomainInfrastructure;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Services
{
    public class PersonUniquenessValidator
    {
        private readonly DataQuery m_DataQuery;
        private readonly ILogger<PersonUniquenessValidator> m_Logger;

        public PersonUniquenessValidator(DataQuery dataQuery, ILogger<PersonUniquenessValidator> logger)
        {
            m_DataQuery = dataQuery;
            m_Logger = logger;
        }

        /// <summary>
        /// Validates LinkedInProfileId is unique when updating a person
        /// </summary>
        /// <param name="person">Person being proposed to Create/Update.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task ValidateUniquePerson(Person person, CancellationToken cancellationToken)
        {
            await ThrowIfPersonExists(person.SearchFirmId, c => c.LinkedInProfileId == person.LinkedInProfileId && 
                                                                    c.Id != person.Id, cancellationToken);

            m_Logger.LogTrace($"No other Person record with '{person.LinkedInProfileId}' is not present, excluding '{person.Id}' and allowed to be used for search firm '{person.SearchFirmId}'.");
        }
        
        private async Task ThrowIfPersonExists(Guid searchFirmId, Expression<Func<Person, bool>> filter, CancellationToken cancellationToken)
        {
            var feedIterator = m_DataQuery.GetFeedIterator<Person>(searchFirmId.ToString(),
                                                                  q => q.Where(filter),
                                                                  1);

            var response = await feedIterator.ReadNextAsync(cancellationToken);
            Console.WriteLine(response.RequestCharge);

            if (response.Any())
                throw new ParamValidationFailureException(nameof(Person.LinkedInProfileUrl), "A record already exists with this {Param}");
        }
    }
}
