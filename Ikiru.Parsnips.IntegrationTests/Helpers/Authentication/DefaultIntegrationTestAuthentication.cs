using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.IntegrationTests.Helpers.HttpMessages;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.IntegrationTests.Helpers.Authentication
{
    public class DefaultIntegrationTestAuthentication
    {
        public static string SubscriptionPlanId { get; } = "talentis-connect-a-gbp";

        private readonly HttpClient m_UnauthClient;
        private readonly CosmosClient m_CosmosClient;

        public Guid DefaultSearchFirmId => GetSearchFirmId();
        public Guid DefaultUserId => DefaultUser.Id;
        public Guid IdentityServerId => Guid.NewGuid();
        public SearchFirm DefaultSearchFirm { get; private set; }
        public SearchFirmUser DefaultUser { get; private set; }
        public Guid DefaultIdentityServerId { get; } = Guid.NewGuid();
         

        public DefaultIntegrationTestAuthentication(HttpClient unauthClient, CosmosClient cosmosClient)
        {
            m_UnauthClient = unauthClient;
            m_CosmosClient = cosmosClient;

            TestDataSetup().GetAwaiter().GetResult();

        }

        private Guid GetSearchFirmId() => DefaultSearchFirm?.Id ?? DefaultUser.SearchFirmId;

        private async Task TestDataSetup()
        {
            await CreateTrialPlan();

            var planTask = CreateSubscriptionPlan(SubscriptionPlanId);
            var signUpTask = EnsureSearchFirmSignedUp();

            await Task.WhenAll(planTask, signUpTask);

            (DefaultSearchFirm, DefaultUser) = signUpTask.Result;
        }

        private async Task<(SearchFirm searchFirm, SearchFirmUser searchFirmUser)> EnsureSearchFirmSignedUp()
        {
            var command = new
            {
                SearchFirmName = "IntegrationTestsSearchFirm_v3",
                SearchFirmCountryCode = "GB",
                SearchFirmPhoneNumber = "01234 56789",
                UserFirstName = "IntegrationTests",
                UserLastName = "User",
                UserEmailAddress = "integration@tests.user",
                UserJobTitle = "Integration Tests Runner",
                UserPassword = "integration_password"
            };

            var existingSearchFirm = await SearchFirmExists(command.SearchFirmName);

            var searchFirmId = existingSearchFirm?.SearchFirmId;
            if (existingSearchFirm == null)
            {
                searchFirmId = await CreateSearchFirm(command);
            }

            var user = await GetSearchFirmUser(searchFirmId.Value, command.UserEmailAddress);

            return (existingSearchFirm, user);

        }


        private async Task<Guid?> CreateSearchFirmInDB(string searchFirmName, string countryCode,
                                                            string phoneNumber)
        {
            var searchFirmcontainer = m_CosmosClient.GetContainer("Parsnips", "SearchFirms");

            var searchFirm = new SearchFirm()
            {
                Name = searchFirmName,
                CountryCode = countryCode,
                PhoneNumber = phoneNumber
            };

            var repsonse = await searchFirmcontainer.CreateItemAsync(searchFirm);

            Assert.Equal(HttpStatusCode.Created, repsonse.StatusCode);

            return searchFirm.Id;
        }

        private async Task<string> CreateSearchFirmUserInDB(string firstName, string lastName,
                                                            string email, string jobTitle,
                                                            Guid searchFirmId)
        {
            var searchFirmUsercontainer = m_CosmosClient.GetContainer("Parsnips", "SearchFirms");

            var searchFirmUser = new SearchFirmUser(searchFirmId)
            {
                FirstName = firstName,
                LastName = lastName,
                EmailAddress = email,
                JobTitle = jobTitle
            };

            var repsonse = await searchFirmUsercontainer.CreateItemAsync(searchFirmUser);

            Assert.Equal(HttpStatusCode.Created, repsonse.StatusCode);

            return searchFirmUsercontainer.Id;
        }


        private async Task<Guid> CreateSearchFirm(object command)
        {
            var response = await m_UnauthClient.PostAsync("/api/searchfirms", new JsonContent(command));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var r = new
            {
                Id = Guid.Empty
            };

            var responseJson = await response.Content.DeserializeToAnonymousType(r);
            Assert.NotNull(responseJson);

            Assert.NotEqual(Guid.Empty, responseJson.Id);
            return responseJson.Id;


        }

        private async Task CreateTrialPlan()
        {
            var container = m_CosmosClient.GetContainer("Parsnips", "Subscriptions");
            var feedIterator = container.GetItemLinqQueryable<ChargebeePlan>(requestOptions: new QueryRequestOptions { MaxItemCount = 1 })
                                        .Where(p => p.Discriminator == ChargebeePlan.PartitionKey && p.PlanType == PlanType.Trial && p.Status == PlanStatus.Active)
                                        .ToFeedIterator();

            var checkResponse = (await feedIterator.ReadNextAsync()).ToList();

            if (checkResponse.Count > 0)
                return;

            var plan = new ChargebeePlan
            {
                PlanId = "Fake-plan-id",
                PlanType = PlanType.Trial,
                Status = PlanStatus.Active
            };

            await container.CreateItemAsync(plan, new PartitionKey(ChargebeePlan.PartitionKey));
        }

        private async Task CreateSubscriptionPlan(string planId)
        {
            var container = m_CosmosClient.GetContainer("Parsnips", "Subscriptions");
            var feedIterator = container.GetItemLinqQueryable<ChargebeePlan>(requestOptions: new QueryRequestOptions { MaxItemCount = 1 })
                                        .Where(p => p.Discriminator == ChargebeePlan.PartitionKey && p.PlanId == planId)
                                        .ToFeedIterator();

            var checkResponse = (await feedIterator.ReadNextAsync()).ToList();

            if (checkResponse.Count > 0)
                return;

            var plan = new ChargebeePlan
            {
                PlanId = planId,
                PlanType = PlanType.Connect,
                CurrencyCode = "GBP",
                Status = PlanStatus.Active
            };

            await container.CreateItemAsync(plan, new PartitionKey(ChargebeePlan.PartitionKey));
        }

        private async Task<SearchFirm> SearchFirmExists(string searchFirmName)
        {
            var container = m_CosmosClient.GetContainer("Parsnips", "SearchFirms");
            var feedIterator = container.GetItemLinqQueryable<SearchFirm>(requestOptions: new QueryRequestOptions { MaxItemCount = 1 })
                                        .Where(s => s.Discriminator == "SearchFirm" && s.Name == searchFirmName)
                                        .ToFeedIterator();

            var checkResponse = (await feedIterator.ReadNextAsync()).ToList();

            if (checkResponse.Count > 1)
                throw new InvalidOperationException("More than one unique item already exists!");

            return checkResponse.SingleOrDefault();
        }

        private async Task<SearchFirmUser> GetSearchFirmUser(Guid searchFirmId, string emailAddress)
        {
            var container = m_CosmosClient.GetContainer("Parsnips", "SearchFirms");
            var feedIterator = container.GetItemLinqQueryable<SearchFirmUser>(requestOptions: new QueryRequestOptions { MaxItemCount = 1 })
                                        .Where(s => s.Discriminator == "SearchFirmUser" &&
                                                    s.SearchFirmId == searchFirmId &&
                                                    s.EmailAddress == emailAddress)
                                        .ToFeedIterator();

            var fetchUserResponse = (await feedIterator.ReadNextAsync()).ToList();

            if (!fetchUserResponse.Any())
                throw new InvalidOperationException("Could not find user expected to exist for Integration Tests!");

            if (fetchUserResponse.Count > 1)
                throw new InvalidOperationException("Found more that one user matching for Integration Tests!");

            return fetchUserResponse.Single();
        }
    }
}