using AutoMapper;
using Ikiru.Parsnips.Application.Infrastructure.Subscription;
using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.UnitTests.Helpers;
using System;
using System.Threading.Tasks;
using Ikiru.Parsnips.Application.Infrastructure.Subscription.Models;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ApiException;
using Moq;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Application.Infrastructure.Subscription
{
    public class CurrentSubscriptionDetailsTests
    {
        private readonly Guid _searchFirmId = Guid.NewGuid();
        private readonly ChargebeeSubscription _paidSubscription;
        private readonly ChargebeeSubscription _trialSubscription;
        private readonly ChargebeePlan _paidPlan;

        private readonly FakeRepository _fakeRepository = new FakeRepository();
        private readonly Mock<ISubscription> _subscriptionMock = new Mock<ISubscription>();

        private RenewalEstimate _renewalEstimate = new RenewalEstimate
        {
            AmountDue = 123456,
            ValueBeforeTax = 100000,
            TaxAmount = 23456,
            Discount = 0,
            CurrencyCode = "GBP",
            NextBillingAt = DateTimeOffset.UtcNow.AddDays(10),
            PlanQuantity = 21
        };

        public CurrentSubscriptionDetailsTests()
        {
            var trialPlan = new ChargebeePlan { PlanId = "trial-plan-id", CurrencyCode = "", Price = 0, Period = 1, PeriodUnit = PeriodUnitEnum.Month, PlanType = PlanType.Trial, Status = PlanStatus.Active };
            _trialSubscription = new ChargebeeSubscription(_searchFirmId) { MainEmail = "trial@email.com", PlanId = trialPlan.PlanId, PlanQuantity = 1, CustomerId = "customer-id-trial", SubscriptionId = "subscription-id-trial", IsEnabled = true, CurrentTermEnd = DateTimeOffset.UtcNow.AddDays(1), Status = Domain.Chargebee.Subscription.StatusEnum.Active };

            _paidPlan = new ChargebeePlan { PlanId = "connect-gbp-month", CurrencyCode = "GBP", Price = 33, Period = 1, PeriodUnit = PeriodUnitEnum.Month, PlanType = PlanType.Connect, Status = PlanStatus.Active };

            var plan1 = new ChargebeePlan { PlanId = "connect-gbp-year", CurrencyCode = "GBP", Price = 301, Period = 1, PeriodUnit = PeriodUnitEnum.Year, PlanType = PlanType.Connect, Status = PlanStatus.Active };
            var plan2 = new ChargebeePlan { PlanId = "connect-eur-year", CurrencyCode = "EUR", Price = 351, Period = 1, PeriodUnit = PeriodUnitEnum.Year, PlanType = PlanType.Basic, Status = PlanStatus.Active };

            _paidSubscription = new ChargebeeSubscription(_searchFirmId) { MainEmail = "test@email.com", PlanId = _paidPlan.PlanId, PlanQuantity = 21, CustomerId = "customer-id-abcd", SubscriptionId = "subscription-id-efgh", IsEnabled = true, CurrentTermEnd = DateTimeOffset.UtcNow.AddDays(1), Status = Domain.Chargebee.Subscription.StatusEnum.Active };

            var inactiveSubscription1 = new ChargebeeSubscription(_searchFirmId) { MainEmail = "test1@email.com", PlanId = plan1.PlanId, PlanQuantity = 5, CustomerId = "customer-id-abcd1", SubscriptionId = "subscription-id-efgh1", IsEnabled = false, CurrentTermEnd = DateTimeOffset.UtcNow.AddDays(1), Status = Domain.Chargebee.Subscription.StatusEnum.Active };
            var inactiveSubscription2 = new ChargebeeSubscription(_searchFirmId) { MainEmail = "test2@email.com", PlanId = plan2.PlanId, PlanQuantity = 6, CustomerId = "customer-id-abcd2", SubscriptionId = "subscription-id-efgh2", IsEnabled = true, CurrentTermEnd = DateTimeOffset.UtcNow.AddDays(-1), Status = Domain.Chargebee.Subscription.StatusEnum.Active };
            var inactiveSubscription3 = new ChargebeeSubscription(_searchFirmId) { MainEmail = "test2@email.com", PlanId = plan2.PlanId, PlanQuantity = 7, CustomerId = "customer-id-abcd3", SubscriptionId = "subscription-id-efgh3", IsEnabled = true, CurrentTermEnd = DateTimeOffset.UtcNow.AddDays(1), Status = Domain.Chargebee.Subscription.StatusEnum.Cancelled };

            _fakeRepository.AddToRepository(trialPlan, _paidPlan, plan1, plan2, _paidSubscription, _trialSubscription, inactiveSubscription1, inactiveSubscription2, inactiveSubscription3);

            _subscriptionMock
               .Setup(s => s.RenewalEstimate(_paidSubscription.SubscriptionId))
               .ReturnsAsync(_renewalEstimate);
        }

        [Fact]
        public async Task GetReturnsCorrectForTrial()
        {
            // Arrange
            _paidSubscription.IsDisabled = true;
            var service = CreateService();

            // Act
            var result = await service.Get(_searchFirmId);

            // Assert
            Assert.Null(result.PaidSubscriptionDetails);
            Assert.NotNull(result.TrialDetails);
            Assert.Equal(_trialSubscription.CurrentTermEnd, result.TrialDetails.TrialEndDate);
        }

        [Theory]
        [InlineData(PlanType.Basic)]
        [InlineData(PlanType.Connect)]
        public async Task GetReturnsCorrectForPaidSubscription(PlanType planType)
        {
            // Arrange
            _paidPlan.PlanType = planType;
            var service = CreateService();

            // Act
            var result = await service.Get(_searchFirmId);

            // Assert
            Assert.Null(result.TrialDetails);
            Assert.NotNull(result.PaidSubscriptionDetails);
            Assert.Equal(planType, result.PaidSubscriptionDetails.PlanType);
            Assert.Equal(_paidPlan.Period, result.PaidSubscriptionDetails.Period);
            Assert.Equal(_paidPlan.PeriodUnit, result.PaidSubscriptionDetails.PeriodUnit);
            Assert.Equal(_paidSubscription.CurrentTermEnd, result.PaidSubscriptionDetails.CurrentTermEnd);

            Assert.Equal(_renewalEstimate.AmountDue, result.PaidSubscriptionDetails.AmountDue);
            Assert.Equal(_renewalEstimate.ValueBeforeTax, result.PaidSubscriptionDetails.ValueBeforeTax);
            Assert.Equal(_renewalEstimate.TaxAmount, result.PaidSubscriptionDetails.TaxAmount);
            Assert.Equal(_renewalEstimate.Discount, result.PaidSubscriptionDetails.Discount);
            Assert.Equal(_renewalEstimate.CurrencyCode, result.PaidSubscriptionDetails.CurrencyCode);
            Assert.Equal(_renewalEstimate.PlanQuantity, result.PaidSubscriptionDetails.PlanQuantity);
            Assert.Equal(_renewalEstimate.NextBillingAt, result.PaidSubscriptionDetails.NextBillingAt);
        }

        [Fact]
        public async Task GetReturnsNoDataWhenSubscriptionExpired()
        {
            // Arrange
            _trialSubscription.IsDisabled = true;
            _paidSubscription.IsDisabled = true;
            var service = CreateService();

            // Act
            var result = await service.Get(_searchFirmId);

            Assert.Null(result.PaidSubscriptionDetails);
            Assert.Null(result.TrialDetails);
        }

        [Fact]
        public async Task GetThrowsWhenChargebeeErrors()
        {
            // Arrange
            _subscriptionMock
               .Setup(s => s.RenewalEstimate(It.IsAny<string>()))
               .ReturnsAsync(new RenewalEstimate { GeneralException = true });
            var service = CreateService();

            // Act
            var ex = await Record.ExceptionAsync(() =>  service.Get(_searchFirmId));

            // Assert
            Assert.NotNull(ex);
            Assert.IsType<ExternalApiException>(ex);
        }

        private CurrentSubscriptionDetails CreateService()
        {
            var config = new MapperConfiguration(cfg => cfg.AddProfile<Parsnips.Application.MappingProfile>());
            var mapper = config.CreateMapper();

            var repo = new SubscriptionRepository(_fakeRepository);
            return new CurrentSubscriptionDetails(repo, _subscriptionMock.Object, mapper);
        }
    }
}
