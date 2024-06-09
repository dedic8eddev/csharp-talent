using AutoMapper;
using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Chargebee;
using Ikiru.Parsnips.Functions.SearchFirmSubscriptionEventHandlers.Coupon;
using Ikiru.Persistence.Repository;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Functions.SearchFirmSubscriptionEventHandlers.Coupon
{
    public class UpdatedTests
    {
        private Mock<IRepository> _respositoryMock;
        private IMapper _mapper;
        private CouponRepository _couponRepository;
        private ChargebeeCoupon _coupon1;
        private Updated.Payload _payload;

        private readonly Guid _existingCouponId = Guid.Parse("74FF5673-779C-4290-A7C3-743FD7F298A7");
        private const string _ffDiscount = "ff-discount-100%";

        public UpdatedTests()
        {
            _mapper = new MapperConfiguration(c =>
                      c.AddProfile<Ikiru.Parsnips.Functions.SearchFirmSubscriptionEventHandlers.Subscription.MappingProfile>()).CreateMapper();
        }

        [Fact]
        public async Task UpdatedInsertsCoupon()
        {
            var newCouponId = "new coupon";

            // Given
            var handler = CreateHandler();
            _payload.Value.Content.Coupon.Id = newCouponId;
            _payload.Value.Content.Coupon.PlanIds = new List<string> { "plan1", "plan2", "plan3" };
            _payload.Value.Content.Coupon.Status = CouponStatusEnum.Active;

            // When
            await handler.Handle(_payload, CancellationToken.None);

            // Then
            _respositoryMock.Verify(r => r.UpdateItem(It.Is<ChargebeeCoupon>(c => AssertCoupons(c, true))));
        }

        private bool AssertCoupons(ChargebeeCoupon chargebeeCoupon, bool isNew)
        {
            var expectedPlanIds = _payload.Value.Content.Coupon.PlanIds;
            _payload.Value.Content.Coupon.Status = CouponStatusEnum.Active;
            expectedPlanIds ??= new List<string>();

            Assert.Equal(expectedPlanIds, chargebeeCoupon.PlanIds);

            var couponIdIsCorrect = isNew ? chargebeeCoupon.Id != Guid.Empty : chargebeeCoupon.Id == _coupon1.Id;
            return couponIdIsCorrect && chargebeeCoupon.CouponId == _payload.Value.Content.Coupon.Id
                && chargebeeCoupon.ApplyAutomatically == (_payload.Value.Content.Coupon.Metadata?.ApplyAutomatically ?? false)
                && chargebeeCoupon.ValidTill == _payload.Value.Content.Coupon.ValidTill;
        }

        [Fact]
        public async Task UpdatedUpdatesCouponIfPresent()
        {
            // Given
            var handler = CreateHandler();
            _respositoryMock.Setup(r => r.GetByQuery(It.IsAny<Expression<Func<ChargebeeCoupon, bool>>>()))
                            .Returns(Task.FromResult(new List<ChargebeeCoupon> { _coupon1 }));

            _payload.Value.Content.Coupon.PlanIds = new List<string> { "plan1", "plan2", "plan3" };
            _payload.Value.Content.Coupon.Status = CouponStatusEnum.Active;

            // When
            await handler.Handle(_payload, CancellationToken.None);

            // Then
            _respositoryMock.Verify(r => r.UpdateItem(It.Is<ChargebeeCoupon>(c => AssertCoupons(c, false))));
        }

        public static IEnumerable<object[]> PlansTestData()
        {
            yield return new object[] { null };
            yield return new object[] { new List<string>() };
            yield return new object[] { new List<string> { "plan-1" } };
            yield return new object[] { new List<string> { "plan-1", "plan-2", "plan-3" } };
        }

        [Theory]
        [MemberData(nameof(PlansTestData))]
        public async Task UpdatedSetsPlanIds(List<string> expectedPlanIds)
        {
            // Given
            var handler = CreateHandler();
            _payload.Value.Content.Coupon.PlanIds = expectedPlanIds;
            _payload.Value.Content.Coupon.Status = CouponStatusEnum.Active;

            // When
            await handler.Handle(_payload, CancellationToken.None);

            // Then
            _respositoryMock.Verify(r => r.UpdateItem(It.Is<ChargebeeCoupon>(c => AssertCoupons(c, true))));
        }

        [Fact]
        public async Task UpdatedThrowsIfCouponPayloadIsNull()
        {
            // Given
            var handler = CreateHandler();
            _payload.Value.Content.Coupon = null;

            // When
            var ex = await Record.ExceptionAsync(() => handler.Handle(_payload, CancellationToken.None));

            // Then
            Assert.NotNull(ex);
            Assert.IsType<ArgumentNullException>(ex);
        }

        [Theory]
        [InlineData(CouponStatusEnum.Archived)]
        [InlineData(CouponStatusEnum.Deleted)]
        [InlineData(CouponStatusEnum.Expired)]
        public async Task UpdatedDeletesCouponIfArchived(CouponStatusEnum status)
        {
            // Given
            var handler = CreateHandler();
            _respositoryMock.Setup(r => r.GetByQuery(It.IsAny<Expression<Func<ChargebeeCoupon, bool>>>()))
                            .Returns(Task.FromResult(new List<ChargebeeCoupon> { _coupon1 }));
            _payload.Value.Content.Coupon.Status = status;

            // When
            await handler.Handle(_payload, CancellationToken.None);

            // Then
            _respositoryMock.Verify(r => r.Delete<ChargebeeCoupon>(It.Is<string>(c => c == _coupon1.Id.ToString()),It.Is<string>(c => c == _coupon1.Id.ToString())));
        }

        public static IEnumerable<object[]> CouponNotDeletedTestData()
            => Array
               .FindAll((CouponStatusEnum[])Enum.GetValues(typeof(CouponStatusEnum)), e => e != CouponStatusEnum.Archived && e != CouponStatusEnum.Deleted && e != CouponStatusEnum.Expired && e != CouponStatusEnum.UnKnown)
                   .Select(e => new object[] { e });

        [Theory]
        [MemberData(nameof(CouponNotDeletedTestData))]
        public async Task UpdatedDoesNotDeleteCouponIfActive(CouponStatusEnum status)
        {
            // Given
            var handler = CreateHandler();
            _respositoryMock.Setup(r => r.GetByQuery(It.IsAny<Expression<Func<ChargebeeCoupon, bool>>>()))
                            .Returns(Task.FromResult(new List<ChargebeeCoupon> { _coupon1 }));
            _payload.Value.Content.Coupon.Status = status;

            // When
            await handler.Handle(_payload, CancellationToken.None);

            // Then
            _respositoryMock.Verify(r => r.Delete<ChargebeeCoupon>(It.IsAny<string>(),It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UpdatedDoesNotDeleteCouponIfMissingFromDb()
        {
            // Given
            var handler = CreateHandler();
            _payload.Value.Content.Coupon.Status = CouponStatusEnum.Archived;

            // When
            await handler.Handle(_payload, CancellationToken.None);

            // Then
            _respositoryMock.Verify(r => r.Delete<ChargebeeCoupon>(It.IsAny<string>(),It.IsAny<string>()), Times.Never);
        }

        private void Setup()
        {
            _payload = new Updated.Payload
            {
                Value = new ChargebeeEventPayload
                {
                    EventType = EventTypeEnum.CouponCreated,
                    Content = new Content
                    {
                        Coupon = new Domain.Chargebee.Coupon { Id = _ffDiscount }
                    }
                }
            };

            _coupon1 = new ChargebeeCoupon
            {
                Id = _existingCouponId,
                CouponId = _ffDiscount,
                Status = Domain.Enums.CouponStatus.Active,
                PlanIds = new List<string> { "plan-id-1", "plan-id-2" },
                ApplyAutomatically = true,
                ValidTill = DateTimeOffset.UtcNow.AddDays(10)
            };

            _respositoryMock = new Mock<IRepository>();

            _respositoryMock.Setup(r => r.Delete<ChargebeeCoupon>(It.Is<string>(x => Guid.Parse(x) == _existingCouponId),It.Is<string>(x => Guid.Parse(x) == _existingCouponId)))
                            .Returns(Task.FromResult(true));
            _respositoryMock.Setup(r => r.Delete<ChargebeeCoupon>(It.IsAny<string>(),It.IsAny<string>()))
                .Returns(Task.FromResult(false));

            _respositoryMock.Setup(r => r.UpdateItem(It.Is<ChargebeeCoupon>(coupon => coupon.Id != _existingCouponId)))
                .Returns((ChargebeeCoupon coupon) => Task.FromResult(coupon));

            _couponRepository = new CouponRepository(_respositoryMock.Object);

            _respositoryMock.Setup(r => r.GetByQuery(It.IsAny<Expression<Func<ChargebeeCoupon, bool>>>()))
                            .Returns(Task.FromResult(new List<ChargebeeCoupon>()));
        }

        private Updated.Handler CreateHandler()
        {
            Setup();

            return new Updated.Handler(_couponRepository, _mapper);
        }
    }
}
