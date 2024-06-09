using Ikiru.Parsnips.Application.Command.Subscription;
using Ikiru.Parsnips.Application.Command.Subscription.Models;
using Ikiru.Parsnips.Application.Infrastructure.Subscription;
using Ikiru.Parsnips.Application.Infrastructure.Subscription.Models;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.UnitTests.Helpers;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Application.Command.Subscription
{
    public class CreateSubscriptionCommandTests
    {
        private readonly string _customerId = "customer-id";
        private readonly string _basicPlanId = "basic-plan-id";
        private readonly string _noDiscountPlanId = "no-discount-plan-id";
        private readonly string _connectPlanId = "connect-plan-id";
        private readonly string _addonId = "addon-id";
        private readonly string _subscriptionId = "subscription-id";

        private readonly int _defaultToken = 19;

        private readonly Mock<ISubscription> _subscriptionMock = new Mock<ISubscription>();

        private readonly CreateSubscriptionRequest _command;

        private readonly FakeRepository _fakeRepository = new FakeRepository();
        private readonly SearchFirm _searchFirm;
        private readonly ChargebeePlan _connectPlan;
        private readonly ChargebeeAddon _tokenAddon;
        private readonly ChargebeeCoupon _coupon;
        private readonly ChargebeeCoupon _userDiscountCoupon;
        private readonly CreatedSubscription _createdSubscription;

        public CreateSubscriptionCommandTests()
        {
            _searchFirm = new SearchFirm { ChargebeeCustomerId = _customerId };

            var user1 = new SearchFirmUser(_searchFirm.Id);
            var user2 = new SearchFirmUser(_searchFirm.Id);
            var disabledUser = new SearchFirmUser(_searchFirm.Id) { IsEnabled = false };

            _connectPlan = new ChargebeePlan
            {
                PlanId = _connectPlanId,
                CurrencyCode = "GBP",
                DefaultTokens = _defaultToken,
                PlanType = PlanType.Connect,
                PeriodUnit = PeriodUnitEnum.Month,
                Period = 1,
                ApplicableAddons = new List<string> { _addonId }
            };
            _tokenAddon = new ChargebeeAddon
            {
                AddonId = _addonId,
                CurrencyCode = _connectPlan.CurrencyCode,
                PeriodUnit = _connectPlan.PeriodUnit,
                Period = _connectPlan.Period,
                Status = AddonStatus.Active,
                AddonType = AddonType.PlanToken
            };

            var basicPlan = new ChargebeePlan { PlanId = _basicPlanId, CurrencyCode = "GBP", PlanType = PlanType.Basic };
            var otherAddon1 = new ChargebeeAddon { AddonId = "other-addon-1", CurrencyCode = "USD", PeriodUnit = PeriodUnitEnum.Month, Status = AddonStatus.Active, Period = 1, AddonType = AddonType.PlanToken };
            var otherAddon2 = new ChargebeeAddon { AddonId = "other-addon-2", CurrencyCode = "GBP", PeriodUnit = PeriodUnitEnum.Month, Status = AddonStatus.UnKnown, Period = 1, AddonType = AddonType.PlanToken };
            var otherAddon3 = new ChargebeeAddon { AddonId = "other-addon-3", CurrencyCode = "GBP", PeriodUnit = PeriodUnitEnum.Month, Status = AddonStatus.Active, Period = 1, AddonType = AddonType.PurchaseToken };
            var otherAddon4 = new ChargebeeAddon { AddonId = "other-addon-4", CurrencyCode = "GBP", PeriodUnit = PeriodUnitEnum.Month, Status = AddonStatus.Deleted, Period = 1, AddonType = AddonType.PlanToken };

            _userDiscountCoupon = new ChargebeeCoupon { CouponId = "coupon-id", ApplyAutomatically = false, ValidTill = DateTimeOffset.UtcNow.AddDays(1), PlanIds = new List<string> { _connectPlanId, _basicPlanId }, Status = CouponStatus.Active };
            var userDiscountCoupon2 = new ChargebeeCoupon { CouponId = "coupon-id-2", ApplyAutomatically = false, ValidTill = DateTimeOffset.UtcNow.AddDays(1), PlanIds = new List<string> { _connectPlanId, _basicPlanId }, Status = CouponStatus.Active };

            _coupon = new ChargebeeCoupon { CouponId = "coupon-id-1", ApplyAutomatically = true, ValidTill = DateTimeOffset.UtcNow.AddDays(1), PlanIds = new List<string> { _connectPlanId }, Status = CouponStatus.Active };
            var coupon2 = new ChargebeeCoupon { CouponId = "coupon-id-2", ApplyAutomatically = false, ValidTill = DateTimeOffset.UtcNow.AddDays(1), PlanIds = new List<string> { _connectPlanId }, Status = CouponStatus.Active };
            var coupon3 = new ChargebeeCoupon { CouponId = "coupon-id-3", ApplyAutomatically = true, ValidTill = DateTimeOffset.UtcNow.AddDays(-1), PlanIds = new List<string> { _connectPlanId }, Status = CouponStatus.Active };
            var coupon4 = new ChargebeeCoupon { CouponId = "coupon-id-4", ApplyAutomatically = true, ValidTill = DateTimeOffset.UtcNow.AddDays(1), PlanIds = new List<string> { _basicPlanId }, Status = CouponStatus.Active };
            var coupon5 = new ChargebeeCoupon { CouponId = "coupon-id-5", ApplyAutomatically = true, ValidTill = DateTimeOffset.UtcNow.AddDays(1), PlanIds = new List<string> { _connectPlanId }, Status = CouponStatus.Archived };

            var noDiscountPlan = new ChargebeePlan { PlanId = _noDiscountPlanId, CurrencyCode = "GBP", PlanType = PlanType.Connect };

            _fakeRepository.AddToRepository(_searchFirm, user1, user2, disabledUser, _connectPlan, _tokenAddon,
                                            basicPlan, otherAddon1, otherAddon2, otherAddon3, otherAddon4,
                                            noDiscountPlan, _userDiscountCoupon, userDiscountCoupon2, _coupon, coupon2, coupon3, coupon4, coupon5);

            _createdSubscription = new CreatedSubscription { SubscriptionId = _subscriptionId, SubscriptionCurrentTermEnd = DateTimeOffset.UtcNow.Date.AddDays(30), SubscriptionStatus = Domain.Chargebee.Subscription.StatusEnum.Active };
            _subscriptionMock.Setup(s => s.CreateSubscriptionForCustomer(It.IsAny<string>(), It.IsAny<CreateSubscriptionRequest>(),
                                                                         It.IsAny<string>(), It.IsAny<int>()))
                             .ReturnsAsync(_createdSubscription);

            _command = new CreateSubscriptionRequest { SearchFirmId = _searchFirm.SearchFirmId, UnitQuantity = 2, SubscriptionPlanId = _connectPlanId, CouponIds = new List<string> { _userDiscountCoupon.CouponId, userDiscountCoupon2.CouponId }};
        }

        [Theory, CombinatorialData]
        public async Task HandleSpecifiesCorrectTokenAddonsForConnect(bool applicableAddonsSet)
        {
            var expectedAddonNumber = _command.UnitQuantity * _defaultToken;

            // Arrange
            if (!applicableAddonsSet)
                _connectPlan.ApplicableAddons = null;

            var createSubscription = CreateSubscription();

            // Act
            await createSubscription.Handle(_command);

            // Assert
            _subscriptionMock.Verify(s => s.CreateSubscriptionForCustomer(It.Is<string>(customerId => customerId == _customerId),
                                                                          It.IsAny<CreateSubscriptionRequest>(),
                                                                          It.Is<string>(a => a == _addonId), expectedAddonNumber));
        }

        [Fact]
        public async Task HandleThrowsIfBadCoupon()
        {
            // Arrange
            _command.CouponIds.Add("bad-coupon");
            var createSubscription = CreateSubscription();

            // Act
            var ex = await Record.ExceptionAsync(() => createSubscription.Handle(_command));

            // Assert
            Assert.NotNull(ex);
            Assert.IsType<ParamValidationFailureException>(ex);
        }

        [Theory, CombinatorialData]
        public async Task HandleDoesNotThrowIfCouponHasLeadingOrTrailingWhiteSpace(bool isLeading)
        {
            Assert.NotEmpty(_command.CouponIds);

            // Arrange
            var coupons = new List<string>();
            foreach (var couponId in _command.CouponIds)
                coupons.Add(isLeading ? "  " + couponId : couponId + "  ");

            _command.CouponIds = coupons;
            var createSubscription = CreateSubscription();

            // Act
            var ex = await Record.ExceptionAsync(() => createSubscription.Handle(_command));

            // Assert
            Assert.Null(ex);
        }

        [Fact]
        public async Task HandleStoresSubscription()
        {
            // Arrange
            var createSubscription = CreateSubscription();

            // Act
            await createSubscription.Handle(_command);

            // Assert
            var stored = await _fakeRepository.GetByQuery<ChargebeeSubscription>(s => s.SubscriptionId == _subscriptionId);
            var subscription = stored.Single();
            Assert.Equal(_command.SubscriptionPlanId, subscription.PlanId);
            Assert.Equal(_searchFirm.ChargebeeCustomerId, subscription.CustomerId);
            Assert.Equal(_createdSubscription.SubscriptionStatus, subscription.Status);
            Assert.Equal(_createdSubscription.SubscriptionCurrentTermEnd, subscription.CurrentTermEnd);
            Assert.True(subscription.IsEnabled);
        }

        [Fact]
        public async Task HandleEnablesSearchFirmIfDisabled()
        {
            // Arrange
            _searchFirm.IsEnabled = false;
            var createSubscription = CreateSubscription();

            // Act
            await createSubscription.Handle(_command);

            // Assert
            var stored = await _fakeRepository.GetByQuery<SearchFirm>(s => s.Id == _command.SearchFirmId);
            var subscription = stored.Single();
            Assert.True(subscription.IsEnabled);
        }

        [Fact]
        public async Task HandleSpecifiesNoTokenAddonsForBasic()
        {
            // Arrange
            _command.SubscriptionPlanId = _basicPlanId;
            var createSubscription = CreateSubscription();

            // Act
            await createSubscription.Handle(_command);

            // Assert
            _subscriptionMock.Verify(s => s.CreateSubscriptionForCustomer(It.Is<string>(customerId => customerId == _customerId),
                                                                          It.IsAny<CreateSubscriptionRequest>(),
                                                                          It.Is<string>(a => a == string.Empty), 0));
        }

        [Fact]
        public async Task HandleSpecifiesNoTokenAddonsWhenApplicableAddonsDontMatch()
        {
            // Arrange
            _tokenAddon.AddonId = "not-matching-addon-id";
            var createSubscription = CreateSubscription();

            // Act
            await createSubscription.Handle(_command);

            // Assert
            _subscriptionMock.Verify(s => s.CreateSubscriptionForCustomer(It.Is<string>(customerId => customerId == _customerId),
                                                                          It.IsAny<CreateSubscriptionRequest>(),
                                                                          It.Is<string>(a => a == string.Empty), It.IsAny<int>()));
        }

        [Theory]
        [InlineData(2)] //same number of users
        [InlineData(3)] //buying 3 subscriptions for 2 users
        public async Task HandleDoesNotThrowWhenBuyingEnoughSubscriptions(int unitQuantity)
        {
            // Arrange
            _command.UnitQuantity = unitQuantity;
            var createSubscription = CreateSubscription();

            // Act
            var ex = await Record.ExceptionAsync(() => createSubscription.Handle(_command));

            // Assert
            Assert.Null(ex);
        }

        [Fact]
        public async Task HandleAutoAddsDiscountWhenApplicableAndNoDiscountsPassed()
        {
            // Arrange
            _command.CouponIds = null;
            var createSubscription = CreateSubscription();

            // Act
            await createSubscription.Handle(_command);

            // Assert
            _subscriptionMock.Verify(s => s.CreateSubscriptionForCustomer(It.Is<string>(customerId => customerId == _customerId),
                                                                          It.Is<CreateSubscriptionRequest>(r => r.CouponIds.Count == 1 && r.CouponIds[0] == _coupon.CouponId),
                                                                          It.IsAny<string>(), It.IsAny<int>()));
        }

        [Fact]
        public async Task HandleDoesNotAddAutoDiscountWhenCouponProvided()
        {
            // Arrange
            Assert.NotEmpty(_command.CouponIds);
            var createSubscription = CreateSubscription();

            // Act
            await createSubscription.Handle(_command);

            // Assert
            _subscriptionMock.Verify(s => s.CreateSubscriptionForCustomer(It.Is<string>(customerId => customerId == _customerId),
                                                                          It.Is<CreateSubscriptionRequest>(r => r.CouponIds.Count == _command.CouponIds.Count
                                                                                                                && r.CouponIds.All(id => _command.CouponIds.Any(c => c == id))),
                                                                          It.IsAny<string>(), It.IsAny<int>()));
        }

        public static IEnumerable<object[]> InvalidAutoDiscountCouponTestData()
        {
            yield return new object[] { new Action<ChargebeeCoupon>(c => c.PlanIds = new List<string> { "dummy-plan-id-1", "dummy-plan-id-2" }) };
            yield return new object[] { new Action<ChargebeeCoupon>(c => c.ApplyAutomatically = false) };
            yield return new object[] { new Action<ChargebeeCoupon>(c => c.ValidTill = DateTimeOffset.UtcNow.AddDays(-1)) };
            yield return new object[] { new Action<ChargebeeCoupon>(c => c.Status = CouponStatus.Archived) };
            yield return new object[] { new Action<ChargebeeCoupon>(c => c.Status = CouponStatus.Deleted) };
            yield return new object[] { new Action<ChargebeeCoupon>(c => c.Status = CouponStatus.Expired) };
            yield return new object[] { new Action<ChargebeeCoupon>(c => c.Status = CouponStatus.Unknown) };
        }

        [Theory]
        [MemberData(nameof(InvalidAutoDiscountCouponTestData))]
        public async Task HandleDoesNotAddAutoDiscountWhenNotApplicable(Action<ChargebeeCoupon> makeCouponInvalid)
        {
            // Arrange
            makeCouponInvalid(_coupon);
            _command.CouponIds = null;
            var createSubscription = CreateSubscription();

            // Act
            await createSubscription.Handle(_command);

            // Assert
            _subscriptionMock.Verify(s => s.CreateSubscriptionForCustomer(It.Is<string>(customerId => customerId == _customerId),
                                                                          It.Is<CreateSubscriptionRequest>(r => r.CouponIds == null),
                                                                          It.IsAny<string>(), It.IsAny<int>()));
        }

        private CreateSubscriptionCommand CreateSubscription()
        {
            return new ServiceBuilder<CreateSubscriptionCommand>()
                  .SetFakeRepository(_fakeRepository)
                  .AddTransient(_subscriptionMock.Object)
                  .Build();
        }
    }
}
