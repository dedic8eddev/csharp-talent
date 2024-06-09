using Ikiru.Parsnips.Application.Infrastructure.DataPool.Models.Person;
using Ikiru.Parsnips.Infrastructure.Datapool.Models;
using Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi;
using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityServerClientAuth;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Infrastructure.Datapool
{
    public class DataPoolAPI : IDataPoolAPI
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IdentityServerBearerTokenRetriever<DataPoolApiSettings> _tokenRetriever;
        private readonly DataPoolApiSettings _dataPoolApiSettings;
        private readonly Lazy<HttpClient> _httpClient;
        private readonly ILogger<DataPoolAPI> _logger;
        private readonly TelemetryClient _telemetryClient;

        public DataPoolAPI(IHttpClientFactory httpClientFactory, IdentityServerBearerTokenRetriever<DataPoolApiSettings> tokenRetriever,
                           IOptions<DataPoolApiSettings> dataPoolApiSettings,
                           ILogger<DataPoolAPI> logger,
                           TelemetryClient telemetryClient)
        {
            _httpClientFactory = httpClientFactory;
            _tokenRetriever = tokenRetriever;
            _dataPoolApiSettings = dataPoolApiSettings.Value;
            _logger = logger;
            _telemetryClient = telemetryClient;
            _httpClient = new Lazy<HttpClient>(() => Initialize());

        }

        public async Task<List<Person>> GetPeronsByWebsiteUrl(string websiteUrl)
        {
            var urlEncoder = UrlEncoder.Create(new TextEncoderSettings());

            try
            {
                HttpResponseMessage response = null;

                response = await _httpClient.Value.GetAsync($"api/v1.0/Persons?url={urlEncoder.Encode(websiteUrl)}");

                if (!response.IsSuccessStatusCode)
                {
                    // not a good way to handle retry token expires, need refactoring
                    SetToken();
                    response = await _httpClient.Value.GetAsync($"api/v1.0/Persons?url={urlEncoder.Encode(websiteUrl)}");
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError($"GetPeronsByWebsiteUrl From datapool error, websiteUrl : {websiteUrl}.  DatapoolAPi repsonse code : {response.StatusCode}");
                    }
                }

                var datapoolContent = await response.Content.ReadAsStringAsync();
                var datapoolPerson = JsonConvert.DeserializeObject<List<Person>>(datapoolContent);

                return datapoolPerson;
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetPeronsByWebsiteUrl From datapool error, websiteUrl : {websiteUrl}");
                throw new Exception($"Unable to 'GetPeronsByWebsiteUrl' from datapool using, websiteUrl : {websiteUrl}.  Exception message - {ex.Message}");
            }
        }

        public async Task<DataPoolPersonSearchResults<Person>> SearchPerson(string personSearchCriteria)
        {
            TrackSearchCriteria(personSearchCriteria);

            try
            {
                HttpResponseMessage response = null;

                var content = new StringContent(personSearchCriteria);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

                response = await _httpClient.Value.PostAsync($"api/v1.0/Persons/search", content);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    // not a good way to handle retry token expires, need refactoring
                    SetToken();
                    response = await _httpClient.Value.PostAsync($"api/v1.0/Persons/search", content);
                }

                var datapoolContent = await response.Content.ReadAsStringAsync();

                var datapoolPerson = JsonConvert.DeserializeObject<DataPoolPersonSearchResults<Person>>(datapoolContent);

                return datapoolPerson;

            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to search for person from data pool. message: {ex.Message}");
            }

        }

        public async Task<Person> SendPersonScraped(JsonDocument person)
        {
            var serialize = System.Text.Json.JsonSerializer.Serialize(person);

            try
            {
                HttpResponseMessage response = null;

                var content = new StringContent(serialize);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

                response = await _httpClient.Value.PutAsync($"api/v1.0/Persons/scraped", content);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    // not a good way to handle retry token expires, need refactoring
                    SetToken();
                    response = await _httpClient.Value.PutAsync($"api/v1.0/Persons/scraped", content);
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"SendPersonScraped to datapool error, person : {person.RootElement}.  DatapoolAPi repsonse code : {response.StatusCode}");
                    return null;
                }

                var datapoolContent = await response.Content.ReadAsStringAsync();

                var datapoolPerson = JsonConvert.DeserializeObject<Person>(datapoolContent);

                return datapoolPerson;
            }
            catch (Exception ex)
            {
                _logger.LogError($"SendPersonScraped From datapool error, person : {person}");
                throw new Exception($"Unable to 'SendPersonScraped' to datapool using scraped data : {person.RootElement}.  Exception message - {ex.Message}");
            }

        }

        private void TrackSearchCriteria(string search)
        {
            var properties = new Dictionary<string, string>
                             {
                                 {"SearchServiceName", "TalentisDatapoolSearch"},
                                 {"SearchString", search}
                             };
            _telemetryClient.TrackEvent("Search", properties);
        }

        private HttpClient Initialize()
        {

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(_dataPoolApiSettings.BaseUrl);
            var token = _tokenRetriever.GetToken().ConfigureAwait(false).GetAwaiter().GetResult();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return httpClient;
        }

        private void SetToken()
        {
            var token = _tokenRetriever.GetToken().ConfigureAwait(false).GetAwaiter().GetResult();
            _httpClient.Value.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

    }
}

