using Ikiru.Parsnips.Api.Controllers.Subscription.Models;
using Ikiru.Parsnips.Application.Command.Subscription.Models;
using Ikiru.Parsnips.Application.Infrastructure.Subscription;
using Ikiru.Parsnips.Application.Query.Subscription.Models;
using Ikiru.Parsnips.IntegrationTests.Helpers;
using Ikiru.Parsnips.IntegrationTests.Helpers.Authentication;
using Ikiru.Parsnips.IntegrationTests.Helpers.HttpMessages;
using Moq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Ikiru.Parsnips.Application.Infrastructure.Subscription.Models;
using Xunit;
using Xunit.Abstractions;
using ChargeBee.Api;
using ChargeBee.Models;
using Newtonsoft.Json.Linq;
using ChargeBee.Exceptions;
using Ikiru.Persistence.Repository;

namespace Ikiru.Parsnips.IntegrationTests.Features.Api.Subscriptions
{
    [Collection(nameof(IntegrationTestCollection))]
    public class SubscriptionTests : IntegrationTestBase, IClassFixture<SubscriptionTests.SubscriptionTestsClassFixture>
    {
        private readonly SubscriptionTestsClassFixture m_ClassFixture;
        private string _paymentIntentId;

        public class SubscriptionTestsClassFixture : IDisposable
        {
            public Mock<ISubscription> _subscription = new Mock<ISubscription>();
            public IntTestServer Server { get; }

            public SubscriptionTestsClassFixture()
            {
                Setup();
                Server = new TestServerBuilder()
                    .AddSingleton(_subscription.Object)
                    .Build();
            }

            private void Setup()
            {
                _subscription.Setup(x => x.GetEstimateForSubscription(It.IsAny<int>(),
                                                                        It.IsAny<string>(),
                                                                        It.IsAny<DateTimeOffset>(),
                                                                        It.IsAny<List<string>>(),
                                                                        It.IsAny<string>(),
                                                                        It.IsAny<string>(),
                                                                        It.IsAny<string>()
                                                                        ))
                    .Returns(Task.FromResult(new Application.Infrastructure.Subscription.Models.SubscriptionEstimate
                    {
                        Discount = 100,
                        Amount = 1500,
                        TaxAmount = 50,
                        Total = 5000,
                        UnitQuantity = 2

                    }));

                _subscription.Setup(x => x.CreatePaymentIntent(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(Task.FromResult(new CreatedPaymentIntent
                    {
                        ReferenceId = "abc123",
                        Gateway = "stripe",
                        Id = "id123gdfsgfsdgfds",
                        Status = CreatedPaymentIntent.StatusEnum.Invited,
                        CurrencyCode = "GBP",
                        Amount = 5000,
                        ExpiresAt = DateTime.Now.AddHours(1),
                        PaymentMethodType = CreatedPaymentIntent.PaymentMethodTypeEnum.Card,
                        CreatedAt = DateTime.Now
                    }));

                _subscription.Setup(x => x.CreateSubscriptionForCustomer(It.IsAny<string>(), It.IsAny<CreateSubscriptionRequest>(), It.IsAny<string>(), It.IsAny<int>()))
                    .Returns(Task.FromResult(new CreatedSubscription() { SubscriptionCurrentTermEnd = DateTimeOffset.Now.AddDays(30) }));
            }

            public void Dispose()
            {
                Server.Dispose();
            }
        }

        public SubscriptionTests(IntegrationTestFixture fixture, SubscriptionTestsClassFixture classFixture, ITestOutputHelper output) : base(fixture, output)
        {
            m_ClassFixture = classFixture;
        }

        [Fact]
        public async Task GetEstimate()
        {

            var estimateRequest = new EstimateRequest
            {
                UnitQuantity = 2,
                BillingAddressCountryCode = "GB",
                BillingAddressZipOrPostCode = "RG24 2tt",
                Couponids = new List<string>() { "launchdiscount", "launchdiscounta" },
                CustomerVatNumber = "abc123456",
                SubscriptionPlanId = "talentis-connect-a-gbp",
                SubscriptionStartDate = DateTimeOffset.Now
            };

            var content = new JsonContent(estimateRequest);

            var response = await m_ClassFixture.Server.Client.PostAsync($"/api/subscription/GetEstimate", content);

            var r = new
            {
                Total = 0,
                Amount = 0,
                Discount = 0,
                TaxAmount = 0,
                InvalidCoupons = new string[] { },
                UnitQuantity = 0
            };

            var responseJson = await response.Content.DeserializeToAnonymousType(r);

            Assert.Contains("launchdiscounta", responseJson.InvalidCoupons);
            Assert.Equal(5000, responseJson.Total);
            Assert.Equal(100, responseJson.Discount);
            Assert.Equal(1500, responseJson.Amount);
            Assert.Equal(50, responseJson.TaxAmount);
            Assert.Equal(2, responseJson.UnitQuantity);
        }


        [Fact]
        public async Task CreatePaymentIntent()
        {
            var paymentIntentRequest = new CreatePaymentIntent
            {
                Amount = 5000,
                CurrencyCode = "GBP",
                UnitQuantity = 2,
                BillingAddressCountryCode = "GB",
                BillingAddressZipOrPostCode = "RG24 2tt",
                Couponids = new List<string>() { "launchdiscount" },
                CustomerVatNumber = "abc123456",
                SubscriptionPlanId = DefaultIntegrationTestAuthentication.SubscriptionPlanId,
                SubscriptionStartDate = DateTimeOffset.Now,
                BillingAddressCity = "southampton",
                BillingAddressLine1 = "my home address line1",
                BillingAddressEmail = "a@a.com"
            };

            var content = new JsonContent(paymentIntentRequest);

            var response = await m_ClassFixture.Server.Client.PostAsync($"/api/subscription/CreatePaymentIntent", content);

            var r = new
            {
                ReferenceId = "",
                Gateway = "",
                Id = "",
                Status = "",
                CurrencyCode = "",
                Amount = "",
                GatewayAccountId = "",
                ExpiresAt = DateTime.Now,
                CustomerId = "",
                PaymentMethodType = "",
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now,
            };

            var responseJson = await response.Content.DeserializeToAnonymousType(r);

            Assert.Equal("abc123", responseJson.ReferenceId);
            Assert.Equal("stripe", responseJson.Gateway);
            Assert.Equal("id123gdfsgfsdgfds", responseJson.Id);
            Assert.Equal("inited", responseJson.Status);
            Assert.Equal("5000", responseJson.Amount);
            Assert.NotEqual(DateTime.MinValue, responseJson.ExpiresAt);
            Assert.Equal("card", responseJson.PaymentMethodType);
            Assert.NotEqual(DateTime.MinValue, responseJson.CreatedAt);


            _paymentIntentId = responseJson.Id;
        }

        [Fact]
        public async Task CreateSubscription()
        {
            await CreatePaymentIntent();

            var createSubscription = new CreateSubscription
            {
                PaymentIntentId = _paymentIntentId,
                SubscriptionPlanId = "talentis-connect-a-gbp",
                UnitQuantity = 10
            };

            var content = new JsonContent(createSubscription);
                
            var response = await m_ClassFixture.Server.Client.PostAsync($"/api/subscription/CreateSubscription", content);


            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task Get()
        {
            var response = await m_ClassFixture.Server.Client.GetAsync($"/api/subscription");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var r = new
            {
                TrialDetails = new
                {
                    TrialEndDate = DateTimeOffset.UtcNow
                }
            };

            var responseJson = await response.Content.DeserializeToAnonymousType(r);

            Assert.Equal(DateTimeOffset.MaxValue, responseJson.TrialDetails.TrialEndDate);
        }

        //
        // IGNORE TEST, ONLY TO BE RUN WHEN YOU NEED TO CREATE A CUSTOMER IN CHARGEBEE
        //[Fact]
        //public async Task CreateCustomerInChargebee()
        //{
        //    ApiConfig.Configure("talentis-test", "test_cdeqHsXcxKzu8ccu3NmnuORslRMqYbLo0Y");

        //    var firstName = "testMark";
        //    var lastName = "testMarklast";
        //    var email = "testMark@test.com";
        //    var searchFirmId = $"0ad8f953-67c6-4cdd-b774-2f183d940462";
        //    var companyName = "testJohnCompanyName";

        //    try
        //    {

        //        var response = await Customer.Create()
        //            .Id(searchFirmId)
        //            .FirstName(firstName)
        //            .LastName(lastName)
        //            .Company(companyName)
        //            .Email(email)
        //            .MetaData(JToken.FromObject(new { searchFirmId }))
        //            .RequestAsync();

        //        var a = response.Customer;
        //    }
        //    catch (InvalidRequestException ex)
        //    {
        //        throw;
        //    }
        //    catch (Exception ex)
        //    {

        //        throw;
        //    }

        //}

    }
}