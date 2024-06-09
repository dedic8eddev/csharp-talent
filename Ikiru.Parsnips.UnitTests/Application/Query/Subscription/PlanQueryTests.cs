using System;
using System.Collections.Generic;
using System.Linq;
using Ikiru.Parsnips.Application.Query.Subscription;
using Ikiru.Parsnips.Application.Query.Subscription.Models;
using System.Threading.Tasks;
using AutoMapper;
using Ikiru.Parsnips.Application.Infrastructure.Subscription;
using Ikiru.Parsnips.Application.Infrastructure.Subscription.Models;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Xunit;
using Ikiru.Parsnips.UnitTests.Helpers;
using Ikiru.Parsnips.Application.Shared.Models;
using Moq;

namespace Ikiru.Parsnips.UnitTests.Application.Query.Subscription
{
    public class PlanQueryTests
    {
        private const string _CURRENCY_CODE_ = "GBP";
        private string _FF_coupon = "ff-coupon";

        private readonly FakeRepository _fakeRepository = new FakeRepository();

        private readonly Mock<ISubscription> _subscriptionMock = new Mock<ISubscription>();

        private readonly ChargebeePlan _plan1 = new ChargebeePlan { PlanId = "plan-id-1", CurrencyCode = _CURRENCY_CODE_, Status = PlanStatus.Active, PlanType = PlanType.Connect };
        private readonly ChargebeePlan _plan2 = new ChargebeePlan { PlanId = "plan-id-2", CurrencyCode = _CURRENCY_CODE_, Status = PlanStatus.Active, PlanType = PlanType.Basic };
        private readonly ChargebeePlan _noDiscountPlan = new ChargebeePlan { PlanId = "no-discount-plan-id", CurrencyCode = _CURRENCY_CODE_, Status = PlanStatus.Active, PlanType = PlanType.Connect };

        private readonly PlanRequest _query = new PlanRequest { Currency = _CURRENCY_CODE_ };

        private readonly SubscriptionEstimate _subscriptionEstimate1;
        private readonly SubscriptionEstimate _subscriptionEstimate2;
        private readonly SubscriptionEstimate _subscriptionEstimate3;
        private readonly SubscriptionEstimate _subscriptionEstimate4;
        private readonly ChargebeeCoupon _coupon1;
        private readonly ChargebeeCoupon _coupon2;

        public PlanQueryTests()
        {
            _subscriptionEstimate1 = new SubscriptionEstimate { Amount = 120, Total = 90, Discount = 25 };
            _subscriptionEstimate2 = new SubscriptionEstimate { Amount = 170, Total = 85, Discount = 50 };
            _subscriptionEstimate3 = new SubscriptionEstimate { Amount = 150, Total = 150, Discount = 0 };
            _subscriptionEstimate4 = new SubscriptionEstimate { Amount = 150, Total = 0, Discount = 100 };

            _coupon1 = new ChargebeeCoupon { CouponId = "coupon-id-1", ApplyAutomatically = true, ValidTill = DateTimeOffset.UtcNow.AddDays(1), PlanIds = new List<string> { _plan1.PlanId }, Status = CouponStatus.Active };
            _coupon2 = new ChargebeeCoupon { CouponId = "coupon-id-2", ApplyAutomatically = true, ValidTill = DateTimeOffset.UtcNow.AddDays(1), PlanIds = new List<string> { _plan2.PlanId }, Status = CouponStatus.Active };

            var coupon4 = new ChargebeeCoupon { CouponId = "coupon-id-4", ApplyAutomatically = true, ValidTill = DateTimeOffset.UtcNow.AddDays(1), PlanIds = new List<string> { _plan1.PlanId }, Status = CouponStatus.Active };
            var coupon5 = new ChargebeeCoupon { CouponId = "coupon-id-5", ApplyAutomatically = false, ValidTill = DateTimeOffset.UtcNow.AddDays(1), PlanIds = new List<string> { _plan1.PlanId }, Status = CouponStatus.Active };
            var coupon6 = new ChargebeeCoupon { CouponId = "coupon-id-6", ApplyAutomatically = true, ValidTill = DateTimeOffset.UtcNow.AddDays(-1), PlanIds = new List<string> { _plan1.PlanId }, Status = CouponStatus.Active };
            var coupon7 = new ChargebeeCoupon { CouponId = "coupon-id-7", ApplyAutomatically = true, ValidTill = DateTimeOffset.UtcNow.AddDays(1), PlanIds = new List<string> { "no-plan" }, Status = CouponStatus.Active };
            var coupon8 = new ChargebeeCoupon { CouponId = "coupon-id-8", ApplyAutomatically = true, ValidTill = DateTimeOffset.UtcNow.AddDays(1), PlanIds = new List<string> { _plan1.PlanId }, Status = CouponStatus.Archived };

            _fakeRepository.AddToRepository(_noDiscountPlan, _plan1, _plan2, _coupon1, _coupon2, coupon4, coupon5, coupon6, coupon7, coupon8);

            _subscriptionMock
               .Setup(s => s.GetEstimateForSubscription(It.IsAny<int>(),
                                                        It.Is<string>(p => p == _plan1.PlanId),
                                                        It.IsAny<DateTimeOffset>(),
                                                        It.Is<List<string>>(c => c.Count == 1 && c[0] == _coupon1.CouponId),
                                                        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
               .ReturnsAsync(_subscriptionEstimate1);

            _subscriptionMock
               .Setup(s => s.GetEstimateForSubscription(It.IsAny<int>(),
                                                        It.Is<string>(p => p == _plan2.PlanId),
                                                        It.IsAny<DateTimeOffset>(),
                                                        It.Is<List<string>>(c => c.Count == 1 && c[0] == _coupon2.CouponId),
                                                        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
               .ReturnsAsync(_subscriptionEstimate2);

            _subscriptionMock
               .Setup(s => s.GetEstimateForSubscription(It.IsAny<int>(),
                                                        It.Is<string>(p => p == _noDiscountPlan.PlanId),
                                                        It.IsAny<DateTimeOffset>(),
                                                        It.Is<List<string>>(c => !c.Any()),
                                                        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
               .ReturnsAsync(_subscriptionEstimate3);

            _subscriptionMock
               .Setup(s => s.GetEstimateForSubscription(It.IsAny<int>(),
                                                        It.IsAny<string>(),
                                                        It.IsAny<DateTimeOffset>(),
                                                        It.Is<List<string>>(c => c.Count == 1 && c[0] == _FF_coupon),
                                                        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
               .ReturnsAsync(_subscriptionEstimate4);
        }

        [Fact]
        public async Task HandlerAddCouponForPlanWhenPresent()
        {
            // Arrange
            var query = CreateQueryHandler();

            // Act
            var result = await query.Handle(_query);

            // Assert
            AssertPrice(_subscriptionEstimate1, result, _plan1.PlanId);
            AssertPrice(_subscriptionEstimate2, result, _plan2.PlanId);
            AssertPrice(_subscriptionEstimate3, result, _noDiscountPlan.PlanId);
        }

        private void AssertPrice(SubscriptionEstimate subscriptionEstimate, PlanResponse response, string planId)
        {
            var result = response.Single(r => r.Id == planId);
            Assert.Equal(subscriptionEstimate.Amount, result.Price.Amount);
            Assert.Equal(subscriptionEstimate.Total, result.Price.Total);
            Assert.Equal(subscriptionEstimate.Discount, result.Price.Discount);
        }

        [Fact]
        public async Task HandlerDoesNotUseCouponWhenACouponIsInTheQuery()
        {
            // Arrange
            _query.Coupons = new List<string> { _FF_coupon };
            var query = CreateQueryHandler();

            // Act
            var result = await query.Handle(_query);

            // Assert
            AssertPrice(_subscriptionEstimate4, result, _plan1.PlanId);
            AssertPrice(_subscriptionEstimate4, result, _plan2.PlanId);
            AssertPrice(_subscriptionEstimate4, result, _noDiscountPlan.PlanId);
        }

        private PlanQuery CreateQueryHandler()
        {
            var profile = new MappingProfile();
            var configuration = new MapperConfiguration(cfg => cfg.AddProfile(profile));
            var mapper = new Mapper(configuration);

            return new ServiceBuilder<PlanQuery>()
                  .SetFakeRepository(_fakeRepository)
                  .AddTransient((IMapper)mapper)
                  .AddTransient(_subscriptionMock.Object)
                  .Build();
        }

        public class MappingProfile : Profile
        {
            public MappingProfile()
            {
                CreateMap<ChargebeePlan, Plan>()
                   .ForMember(dest => dest.Id, src => src.MapFrom(p => p.PlanId.ToString()))
                   .ForMember(dest => dest.Price, src => src.Ignore());
                //.ForMember(dest => dest.Price, src => src.MapFrom(p => p.Price));
            }
        }
    }
}
