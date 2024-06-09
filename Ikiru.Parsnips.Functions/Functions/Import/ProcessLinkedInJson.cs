using Ikiru.Parsnips.Domain;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Functions.Functions.Import
{
    public class ProcessLinkedInJson
    {
        private const string _PROFILE_TYPE = "com.linkedin.voyager.dash.identity.profile.Profile";
        private const string _FULL_PROFILE_RECIPE_TYPE = "com.linkedin.voyager.dash.deco.identity.profile.FullProfileWithEntities";
        private const string _POSITION_TYPE = "com.linkedin.voyager.dash.identity.profile.Position";
        private const string _COMPANY_TYPE = "com.linkedin.voyager.dash.organization.Company";

        private readonly ILogger<ProcessLinkedInJson> m_Logger;

        public ProcessLinkedInJson(ILogger<ProcessLinkedInJson> logger)
        {
            m_Logger = logger;
        }

        public async Task Import(Person person, ImportBlob importBlob, Func<Person, Task<Person>> persistAction)
        {
            var bytes = Convert.FromBase64String(importBlob.Base64Document);

            using var jsonDoc = JsonDocument.Parse(bytes);
            var dataArray = jsonDoc.RootElement.GetProperty("included").EnumerateArray();

            ReadMainProfileData(person, dataArray);
            ReadPositionData(person, dataArray);

            await persistAction(person);
        }
        
        private void ReadMainProfileData(Person person, JsonElement.ArrayEnumerator dataArray)
        {
            var mainProfileData = dataArray.Single(e => e.GetProperty("$type").GetString() == _PROFILE_TYPE &&
                                                        e.GetProperty("$recipeTypes").EnumerateArray().Any(r => r.GetString() == _FULL_PROFILE_RECIPE_TYPE));

            var mainProfile = ToObject<LinkedInMainProfile>(mainProfileData);

            person.Name = $"{mainProfile.firstName} {mainProfile.lastName}";
            person.Location = mainProfile.locationName;
            person.ImportedLinkedInProfileUrl = mainProfile.publicIdentifier;
        }

        private void ReadPositionData(Person person, JsonElement.ArrayEnumerator dataArray)
        {
            var jobs = dataArray.Where(e => e.GetProperty("$type").GetString() == _POSITION_TYPE);

            var positions = jobs.Select(e => ToObject<LinkedInPosition>(e)).ToList();
            m_Logger.LogDebug($"{positions.Count} Position records found for {person.Id} [{person.ImportId}]");

            var currentJob = positions.OrderByDescending(p => p.dateRange?.end == null)
                                      .ThenByDescending(p => p.dateRange?.end?.year)
                                      .ThenByDescending(p => p.dateRange?.end?.month)
                                      .ThenByDescending(p => p.dateRange?.start?.year)
                                      .ThenByDescending(p => p.dateRange?.start?.month)
                                      .FirstOrDefault();

            if (currentJob == null)
                return;

            person.JobTitle = currentJob.title;
            person.Organisation = currentJob.companyName;

            ReadCompanyData(person, currentJob, dataArray);
        }

        private void ReadCompanyData(Person person, LinkedInPosition currentJob, JsonElement.ArrayEnumerator dataArray)
        {
            if (string.IsNullOrWhiteSpace(currentJob.companyUrn))
                return;

            var currentCompany = dataArray.Where(e => e.GetProperty("$type").GetString() == _COMPANY_TYPE)
                                          .Select(e => ToObject<LinkedInCompany>(e))
                                          .FirstOrDefault(c => string.Compare(c.entityUrn, currentJob.companyUrn, StringComparison.InvariantCultureIgnoreCase) == 0);

            if (currentCompany == null)
            {
                m_Logger.LogWarning($"Json Import for Person '{person.Id}': Expected Company entry matching '{currentJob.companyUrn}' for Current Job but none found.");
                return;
            }

            person.ImportedLinkedInCompanyUrl = currentCompany.url;
        }

        #region De-Serialisation 

        private static T ToObject<T>(JsonElement element, JsonSerializerOptions options = null)
        {
            var bufferWriter = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8JsonWriter(bufferWriter))
                element.WriteTo(writer);
            return JsonSerializer.Deserialize<T>(bufferWriter.WrittenSpan, options);
        }
        
        // ReSharper disable InconsistentNaming - Json de-serialisation
#pragma warning disable IDE1006 // Naming Styles

        public class LinkedInMainProfile
        {
            public string publicIdentifier { get; set; }
            public string firstName { get; set; }
            public string lastName { get; set; }
            public string locationName { get; set; }
        }

        public class LinkedInPosition
        {
            public string companyName { get; set; }
            public string title { get; set; }
            public DateRange dateRange { get; set; }
            public string companyUrn { get; set; }

            public class DateRange
            {
                public Date start { get; set; }
                public Date end { get; set; }

                public class Date
                {
                    public int month { get; set; }
                    public int year { get; set; }
                }
            }
        }

        public class LinkedInCompany
        {
            public string entityUrn { get; set; }
            public string url { get; set; }
        }
        
#pragma warning restore IDE1006 // Naming Styles
        // ReSharper restore InconsistentNaming

        #endregion
    }
}
