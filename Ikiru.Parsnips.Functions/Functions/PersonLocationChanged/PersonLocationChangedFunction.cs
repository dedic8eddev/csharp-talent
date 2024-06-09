using System.Linq;
using System.Text.Json;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.DomainInfrastructure;
using Ikiru.Parsnips.Functions.Maps;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues.Items;
using Microsoft.Extensions.Options;
using Microsoft.Azure.Storage.Queue;

namespace Ikiru.Parsnips.Functions.Functions.PersonLocationChanged
{
    public class PersonLocationChangedFunction
    {
        private static readonly string[] s_ExpectedEntityTypes = { null, "MunicipalitySubdivision", "Municipality", "CountrySecondarySubdivision", "CountrySubdivision", "Country" };

        private readonly DataStore m_DataStore;
        private readonly IAzureMaps m_AzureMaps;
        private readonly AzureMapsSettings m_Settings;

        public PersonLocationChangedFunction(DataStore dataStore, IAzureMaps azureMaps, IOptions<AzureMapsSettings> settings)
        {
            m_DataStore = dataStore;
            m_AzureMaps = azureMaps;
            m_Settings = settings.Value;
        }
        
        [FunctionName(nameof(PersonLocationChangedFunction))]
        public async Task Run([QueueTrigger(QueueStorage.QueueNames.PersonLocationChangedQueue)] CloudQueueMessage queueMessage, ILogger log)
        {
            if (queueMessage.DequeueCount > 1)
                log.LogWarning($"Message {queueMessage.Id} has been dequeued multiple times. This is dequeue '{queueMessage.DequeueCount}'");
                
            var queueItem = JsonSerializer.Deserialize<PersonLocationChangedQueueItem>(queueMessage.AsString);

            var person = await m_DataStore.Fetch<Person>(queueItem.PersonId, queueItem.SearchFirmId);

            await GeolocatePersonLocation(log, person);
            await m_DataStore.Update(person); // TODO: Concurrency check - overwrite possible if person edited between Fetch and Update.
        }

        private async Task GeolocatePersonLocation(ILogger log, Person person)
        {
            if (!person.HasLocation())
            {
                person.RemoveGeolocation();
                return;
            }

            var mapsResult = await m_AzureMaps.SearchAddress(m_Settings.ApiKey, person.Location);

            log.LogInformation($"Person '{person.Id}' matched '{mapsResult.results.Length}' address results.");

            var firstResult = mapsResult.results.FirstOrDefault();
            if (firstResult == null)
            {
                person.RemoveGeolocation();
            }
            else
            {
                person.SetGeolocation(firstResult.position.lon, firstResult.position.lat, Flatten(firstResult.address));
                log.LogDebug($"Person '{person.Id}' first result matched entityType: '{firstResult.entityType}' [{person.GeolocationDescription}]");
            }

            if (!s_ExpectedEntityTypes.Contains(firstResult?.entityType))
                log.LogWarning($"Person '{person.Id}' updating with Geolocation; Result entityType was unexpected '{firstResult?.entityType ?? "<null>"}'.");
        }

        private static string Flatten(SearchAddressResponse.Address address)
        {
            return string.Join(", ",
                               new[]
                                   {
                                       address.municipalitySubdivision,
                                       address.municipality,
                                       address.countrySecondarySubdivision,
                                       address.countrySubdivisionName,
                                       address.country
                                   }
                                  .Where(s => !string.IsNullOrEmpty(s)));
        }
    }
}
