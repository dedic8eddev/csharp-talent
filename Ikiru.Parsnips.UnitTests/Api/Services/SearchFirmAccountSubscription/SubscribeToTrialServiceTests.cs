using Ikiru.Parsnips.Api.Services.SearchFirmAccountSubscription;
using Ikiru.Parsnips.Application.Command.Subscription.Models;
using Ikiru.Parsnips.Application.Infrastructure.Subscription;
using Ikiru.Parsnips.Application.Infrastructure.Subscription.Models;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.UnitTests.Helpers;
using Microsoft.Azure.Cosmos;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Customer = Ikiru.Parsnips.Application.Infrastructure.Subscription.Models.Customer;
using Subscription = Ikiru.Parsnips.Domain.Chargebee.Subscription;

namespace Ikiru.Parsnips.UnitTests.Api.Services.SearchFirmAccountSubscription
{
    public class SubscribeToTrialServiceTests
    {
        private const string _CUSTOMER_ID = "abcd-customer-id";
        private const string _SUBSCRIPTION_ID = "efgh-subscription-id";
        private const string _SEARCH_FIRM_NAME = "Fruity Broccoli agency";
        private const string _COUNTRY_CODE = "FR";

        private const string _EMAIL = "Amélie.Poulain@example.com";
        private const string _FIRST_NAME = "Amélie";
        private const string _LAST_NAME = "Poulain";

        private const Subscription.StatusEnum _STATUS = Subscription.StatusEnum.NonRenewing;
        private readonly FakeCosmos m_FakeCosmos = new FakeCosmos();
        private readonly ChargebeePlan m_ChargebeePlan;
        private readonly Mock<ISubscription> m_MockSubscription = new Mock<ISubscription>();
        private readonly SearchFirmAccountTrialSubscriptionModel m_SubscriptionModel;

        private readonly ServiceBuilder<SubscribeToTrialService> m_Service = new ServiceBuilder<SubscribeToTrialService>();
        private readonly SearchFirm m_SearchFirm;
        private readonly DateTimeOffset? m_TermEnd = DateTimeOffset.UtcNow.AddDays(15);

        public SubscribeToTrialServiceTests()
        {
            m_ChargebeePlan = new ChargebeePlan { PlanId = "chargebee-gbp-1month", PlanType = Domain.Enums.PlanType.Trial };

            m_SearchFirm = new SearchFirm
            {
                CountryCode = _COUNTRY_CODE,
                PhoneNumber = "0123456",
                IsEnabled = true,
                Name = _SEARCH_FIRM_NAME
            };

            m_SubscriptionModel = new SearchFirmAccountTrialSubscriptionModel
            {
                SearchFirmId = m_SearchFirm.Id,
                CustomerFirstName = _FIRST_NAME,
                CustomerLastName = _LAST_NAME,
                MainEmail = _EMAIL
            };

            m_FakeCosmos
               .EnableContainerLinqQuery<ChargebeePlan, string>(FakeCosmos.ChargebeeContainerName, ChargebeePlan.PartitionKey, () => new List<ChargebeePlan> { m_ChargebeePlan })
               .EnableContainerFetch(FakeCosmos.SearchFirmsContainerName, m_SearchFirm.Id.ToString(), m_SearchFirm.Id.ToString(), () => m_SearchFirm)
               .EnableContainerReplace<SearchFirm>(FakeCosmos.SearchFirmsContainerName, m_SearchFirm.Id.ToString(), m_SearchFirm.Id.ToString())
               .EnableContainerInsert<ChargebeeSubscription>(FakeCosmos.ChargebeeContainerName);

            m_MockSubscription
               .Setup(s => s.CreateCustomer(It.Is<Customer>(c => c.FirstName == _FIRST_NAME && c.LastName == _LAST_NAME && c.SearchFirmId == m_SearchFirm.Id && c.MainEmail == _EMAIL && c.SearchFirmName == _SEARCH_FIRM_NAME)))
               .ReturnsAsync(_CUSTOMER_ID);

            m_MockSubscription
               .Setup(s => s.CreateSubscriptionForCustomer(It.Is<string>(c => c == _CUSTOMER_ID), It.Is<CreateSubscriptionRequest>(r => r.SearchFirmId == m_SearchFirm.Id && r.SubscriptionPlanId == m_ChargebeePlan.PlanId && r.UnitQuantity == 1), It.IsAny<string>(), It.IsAny<int>()))
               .ReturnsAsync(new CreatedSubscription { SubscriptionStatus = _STATUS, SubscriptionCurrentTermEnd = m_TermEnd, SubscriptionId = _SUBSCRIPTION_ID });
        }

        [Fact]
        public async Task ServiceCreatesCustomer()
        {
            // Given
            var service = CreateService();

            // When
            await service.SubscribeToTrial(m_SubscriptionModel);

            // Then
            m_MockSubscription
               .Verify(s => s.CreateCustomer(It.Is<Customer>(customer => AssertRequest(customer))));
        }

        private bool AssertRequest(Customer customer)
        {
            Assert.Equal(_FIRST_NAME, customer.FirstName);
            Assert.Equal(_LAST_NAME, customer.LastName);
            Assert.Equal(_EMAIL, customer.MainEmail);
            Assert.Equal(_SEARCH_FIRM_NAME, customer.SearchFirmName);
            Assert.Equal(_COUNTRY_CODE, customer.CountryCode);
            Assert.Equal(m_SearchFirm.Id, customer.SearchFirmId);

            return true;
        }

        [Fact]
        public async Task ServiceCreatesSubscription()
        {
            // Given
            var service = CreateService();

            // When
            await service.SubscribeToTrial(m_SubscriptionModel);

            // Then
            m_MockSubscription
               .Verify(s => s.CreateSubscriptionForCustomer(
                                                            It.Is<string>(c => c == _CUSTOMER_ID),
                                                            It.Is<CreateSubscriptionRequest>(r => r.SearchFirmId == m_SearchFirm.Id 
                                                                                                  && r.SubscriptionPlanId == m_ChargebeePlan.PlanId 
                                                                                                  && r.UnitQuantity == 1),
                                                            null, 0));
        }

        [Fact]
        public async Task ServiceStoresSubscriptionDetails()
        {
            // Given
            var service = CreateService();

            // When
            await service.SubscribeToTrial(m_SubscriptionModel);

            // Then
            var container = m_FakeCosmos.ChargebeeContainer;
            container.Verify(c => c.CreateItemAsync(
                                                    It.Is<ChargebeeSubscription>(s => s.PlanId == m_ChargebeePlan.PlanId &&
                                                                                      s.CustomerId == _CUSTOMER_ID &&
                                                                                      s.SubscriptionId == _SUBSCRIPTION_ID &&
                                                                                      s.MainEmail == m_SubscriptionModel.MainEmail &&
                                                                                      s.IsEnabled &&
                                                                                      s.Status == Subscription.StatusEnum.NonRenewing &&
                                                                                      s.CurrentTermEnd == m_TermEnd ),
                                                    It.Is<PartitionKey?>(k => k == new PartitionKey(m_SubscriptionModel.SearchFirmId.ToString())),
                                                    It.IsAny<ItemRequestOptions>(),
                                                    It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task ServiceUpdatesSearchFirmWithCustomerId()
        {
            // Given
            var service = CreateService();

            // When
            await service.SubscribeToTrial(m_SubscriptionModel);

            // Then
            var container = m_FakeCosmos.SearchFirmsContainer;

            container.Verify(c => c.ReplaceItemAsync(
                                                    It.Is<SearchFirm>(s => s.SearchFirmId == m_SubscriptionModel.SearchFirmId &&
                                                                           s.ChargebeeCustomerId == _CUSTOMER_ID &&
                                                                           s.CountryCode == m_SearchFirm.CountryCode &&
                                                                           s.PhoneNumber == m_SearchFirm.PhoneNumber &&
                                                                           s.IsEnabled == m_SearchFirm.IsEnabled),
                                                    It.Is<string>(id => id == m_SubscriptionModel.SearchFirmId.ToString()),
                                                    It.Is<PartitionKey?>(k => k == new PartitionKey(m_SubscriptionModel.SearchFirmId.ToString())),
                                                    It.IsAny<ItemRequestOptions>(),
                                                    It.IsAny<CancellationToken>()));
        }

        private SubscribeToTrialService CreateService()
        {
            return m_Service.SetFakeCosmos(m_FakeCosmos)
                                    .AddTransient(m_MockSubscription.Object)
                                    .Build();
        }
    }
}
