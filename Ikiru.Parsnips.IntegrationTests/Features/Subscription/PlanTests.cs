using Ikiru.Parsnips.Api.Controllers.Subscription;
using Ikiru.Parsnips.IntegrationTests.Helpers;
using Ikiru.Parsnips.IntegrationTests.Helpers.HttpMessages;
using Ikiru.Parsnips.Domain;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Ikiru.Parsnips.IntegrationTests.Features.Subscription
{
    [Collection(nameof(IntegrationTestCollection))]
    public class PlanTests : IntegrationTestBase, IClassFixture<PlanTests.PlanTestsClassFixture>
    {
        private readonly PlanTestsClassFixture _ClassFixture;
        private ChargebeePlan _ChargebeePlan;
        private ChargebeePlan _ChargebeePlan2;
        private ChargebeePlan _ChargebeePlan3;

        public class PlanTestsClassFixture : IDisposable
        {
            public IntTestServer Server { get; }

            // ReSharper disable once UnusedParameter.Local
            public PlanTestsClassFixture()
            {
                Server = new TestServerBuilder()  
                   .Build();
            }

            public void Dispose()
            {
                //Remove test data
                var cosmosClient = this.Server.GetCosmosContainer("Subscriptions");

                cosmosClient.DeleteItemAsync<ChargebeePlan>("74FF5673-779C-4290-A7C3-743FD7F298A7", new PartitionKey(ChargebeePlan.PartitionKey));
                cosmosClient.DeleteItemAsync<ChargebeePlan>("16933B88-172D-4C0D-BD3F-9BF2EB492441", new PartitionKey(ChargebeePlan.PartitionKey));
                cosmosClient.DeleteItemAsync<ChargebeePlan>("16933B88-172D-4C0D-BD3F-9BF2EB492451", new PartitionKey(ChargebeePlan.PartitionKey));
                Server.Dispose();
            }
        }

        public PlanTests(IntegrationTestFixture fixture, PlanTestsClassFixture classFixture, ITestOutputHelper output) : base(fixture, output)
        {
            _ClassFixture = classFixture;

            _ChargebeePlan = new ChargebeePlan()
            {
                Id = Guid.Parse("74FF5673-779C-4290-A7C3-743FD7F298A7"),
                PlanId = "NewPlan",
                PeriodUnit = Domain.Enums.PeriodUnitEnum.Year,
                Period = 1,
                PlanType = Domain.Enums.PlanType.Basic,
                CurrencyCode = "XSD",
                Price = 1300,
                CanPurchaseRocketReach = true,
                DefaultTokens = 40,
                Status = Domain.Enums.PlanStatus.Active
            };

            _ChargebeePlan2 = new ChargebeePlan()
            {
                Id = Guid.Parse("16933B88-172D-4C0D-BD3F-9BF2EB492441"),
                PlanId = "NewPlan2",
                PeriodUnit = Domain.Enums.PeriodUnitEnum.Month,
                Period = 1,
                PlanType = Domain.Enums.PlanType.Basic,
                CurrencyCode = "XSD",
                Price = 80,
                CanPurchaseRocketReach = false,
                DefaultTokens = 0,
                Status = Domain.Enums.PlanStatus.Active
            };

            _ChargebeePlan3 = new ChargebeePlan()
            {
                Id = Guid.Parse("16933B88-172D-4C0D-BD3F-9BF2EB492451"),
                PlanId = "NewPlan3",
                PeriodUnit = Domain.Enums.PeriodUnitEnum.Month,
                Period = 1,
                PlanType = Domain.Enums.PlanType.Basic,
                CurrencyCode = "XBP",
                Price = 80,
                CanPurchaseRocketReach = false,
                DefaultTokens = 0,
                Status = Domain.Enums.PlanStatus.Active
            };



            SaveTestDataToCosmos().Wait();
        }



        private async Task SaveTestDataToCosmos()
        {
            var cosmosClient = _ClassFixture.Server.GetCosmosContainer("Subscriptions");

            await cosmosClient.UpsertItemAsync(_ChargebeePlan, new PartitionKey(ChargebeePlan.PartitionKey));
            await cosmosClient.UpsertItemAsync(_ChargebeePlan2, new PartitionKey(ChargebeePlan.PartitionKey));
            await cosmosClient.UpsertItemAsync(_ChargebeePlan3, new PartitionKey(ChargebeePlan.PartitionKey));
        }

        [Fact]
        public async Task GetPlansReturnsListOfPlans()
        {
            //Given
            var plansRequest = new PlansRequest()
            {
                Currency = "xsd"
            };
   
            var content = new JsonContent(plansRequest);

            //When
            var webResponse = await _ClassFixture.Server.Client.PostAsync($"/api/subscription/plans",content);

            Assert.Equal(HttpStatusCode.OK, webResponse.StatusCode);

            string stringResponse = await webResponse.Content.ReadAsStringAsync();

            var response = JsonDocument.Parse(stringResponse);
            //
            Assert.Equal(2, response.RootElement.GetArrayLength());
            Assert.Equal(_ChargebeePlan.PlanId.ToString().ToLower(), response.RootElement[0].GetProperty("id").GetString().ToLower());
            Assert.Equal(_ChargebeePlan2.PlanId.ToString().ToLower(), response.RootElement[1].GetProperty("id").GetString().ToLower());
        }


        [Fact]
        public async Task GetPlansSelectsPlansByCurrency()
        {
            //Given
            var plansRequest = new PlansRequest()
            {
                Currency ="xbp"
            };
            var content = new JsonContent(plansRequest);

            //When
            var webResponse = await _ClassFixture.Server.Client.PostAsync($"/api/subscription/plans", content);

            Assert.Equal(HttpStatusCode.OK, webResponse.StatusCode);

            string stringResponse = await webResponse.Content.ReadAsStringAsync();

            var response = JsonDocument.Parse(stringResponse);
            //
            Assert.Equal(1, response.RootElement.GetArrayLength());
            Assert.Equal(_ChargebeePlan3.PlanId.ToString().ToLower(), response.RootElement[0].GetProperty("id").GetString().ToLower());
        }

        [Fact]
        public async Task GetPlanWithCouponCallsEstimate()
        {

            //Given
            var plansRequest = new PlansRequest()
            {
                Currency = "xbp",
                Coupons = new List<string>() { "Discount50" }
            };
            var content = new JsonContent(plansRequest);

            //When
            var webResponse = await _ClassFixture.Server.Client.PostAsync($"/api/subscription/plans", content);

            Assert.Equal(HttpStatusCode.OK, webResponse.StatusCode);

            string stringResponse = await webResponse.Content.ReadAsStringAsync();

            var response = JsonDocument.Parse(stringResponse);

            Assert.Equal(1, response.RootElement[0].GetProperty("price").GetProperty("invalidCoupons").GetArrayLength());
        }
    }




}