using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Chargebee;
using Ikiru.Parsnips.UnitTests.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Application.Persistence
{
    public class SubscriptionRepositoryTests
    {
        private readonly FakeRepository _fakeRepository = new FakeRepository();

        private readonly Guid _searchFirmId = Guid.NewGuid();

        private readonly List<ChargebeeSubscription> _chargebeeSubscriptions;

        public SubscriptionRepositoryTests()
        {
            var chargebeeSubscription1 = new ChargebeeSubscription(_searchFirmId) { PlanId = "plan-id", CustomerId = "customer-id", SubscriptionId = "subscription-id", MainEmail = "main@subscription.email", Status = Subscription.StatusEnum.NonRenewing, CurrentTermEnd = DateTimeOffset.UtcNow.AddDays(1) };
            _fakeRepository.AddToRepository(chargebeeSubscription1.Id, chargebeeSubscription1);

            var chargebeeSubscription2 = new ChargebeeSubscription(_searchFirmId) { PlanId = "plan-id-2", CustomerId = "customer-id-2", SubscriptionId = "subscription-id-2", MainEmail = "main@subscription2.email", Status = Subscription.StatusEnum.Active, CurrentTermEnd = DateTimeOffset.UtcNow.AddDays(1) };
            _fakeRepository.AddToRepository(chargebeeSubscription2.Id, chargebeeSubscription2);

            var disabledSubscription = new ChargebeeSubscription(_searchFirmId) { IsDisabled = true, PlanId = "plan-id-disabled", CustomerId = "customer-id-2", SubscriptionId = "subscription-id-2", MainEmail = "main@subscription2.email", Status = Subscription.StatusEnum.Active, CurrentTermEnd = DateTimeOffset.UtcNow.AddDays(1) };
            _fakeRepository.AddToRepository(disabledSubscription.Id, disabledSubscription);

            var expiredSubscription = new ChargebeeSubscription(_searchFirmId) { CurrentTermEnd = DateTimeOffset.UtcNow.AddDays(-1), PlanId = "plan-id-expired", CustomerId = "customer-id-2", SubscriptionId = "subscription-id-2", MainEmail = "main@subscription2.email", Status = Subscription.StatusEnum.Active };
            _fakeRepository.AddToRepository(expiredSubscription.Id, expiredSubscription);

            var wrongStatusSubscription1 = new ChargebeeSubscription(_searchFirmId) { PlanId = "plan-id-wrong-status", Status = Subscription.StatusEnum.UnKnown, CustomerId = "customer-id", SubscriptionId = "subscription-id", MainEmail = "main@subscription.email", CurrentTermEnd = DateTimeOffset.UtcNow.AddDays(1) };
            _fakeRepository.AddToRepository(wrongStatusSubscription1.Id, wrongStatusSubscription1);
            var wrongStatusSubscription2 = new ChargebeeSubscription(_searchFirmId) { PlanId = "plan-id-wrong-status", Status = Subscription.StatusEnum.Future, CustomerId = "customer-id", SubscriptionId = "subscription-id", MainEmail = "main@subscription.email", CurrentTermEnd = DateTimeOffset.UtcNow.AddDays(1) };
            _fakeRepository.AddToRepository(wrongStatusSubscription2.Id, wrongStatusSubscription2);
            var wrongStatusSubscription3 = new ChargebeeSubscription(_searchFirmId) { PlanId = "plan-id-wrong-status", Status = Subscription.StatusEnum.Paused, CustomerId = "customer-id", SubscriptionId = "subscription-id", MainEmail = "main@subscription.email", CurrentTermEnd = DateTimeOffset.UtcNow.AddDays(1) };
            _fakeRepository.AddToRepository(wrongStatusSubscription3.Id, wrongStatusSubscription3);
            var wrongStatusSubscription4 = new ChargebeeSubscription(_searchFirmId) { PlanId = "plan-id-wrong-status", Status = Subscription.StatusEnum.Cancelled, CustomerId = "customer-id", SubscriptionId = "subscription-id", MainEmail = "main@subscription.email", CurrentTermEnd = DateTimeOffset.UtcNow.AddDays(1) };
            _fakeRepository.AddToRepository(wrongStatusSubscription4.Id, wrongStatusSubscription4);

            var otherSearchFirmchargebeeSubscription = new ChargebeeSubscription(Guid.NewGuid()) { PlanId = "plan-id-3", CustomerId = "customer-id-3", SubscriptionId = "subscription-id-3", MainEmail = "main@subscription3.email", Status = Subscription.StatusEnum.Active, CurrentTermEnd = DateTimeOffset.UtcNow.AddDays(1) };
            _fakeRepository.AddToRepository(otherSearchFirmchargebeeSubscription.Id, otherSearchFirmchargebeeSubscription);

            _chargebeeSubscriptions = new List<ChargebeeSubscription> { chargebeeSubscription2, chargebeeSubscription1 };
        }

        [Fact]
        public async Task GetSubscriptionsForSearchFirmReturnsCorrectResult()
        {
            // Arrange
            var repository = CreateRepository();

            // Act
            var result = await repository.GetActiveSubscriptionsForSearchFirm(_searchFirmId);

            // Assert
            Assert.Equal(_chargebeeSubscriptions, result, new SubscriptionComparer());
        }

        private class SubscriptionComparer : IEqualityComparer<ChargebeeSubscription>
        {
            public bool Equals(ChargebeeSubscription x, ChargebeeSubscription y)
            {
                if (ReferenceEquals(x, y))
                    return true;
                if (ReferenceEquals(x, null))
                    return false;
                if (ReferenceEquals(y, null))
                    return false;
                if (x.GetType() != y.GetType())
                    return false;
                return x.MainEmail == y.MainEmail && x.PlanId == y.PlanId &&
                       x.CustomerId == y.CustomerId && x.SubscriptionId == y.SubscriptionId &&
                       x.IsDisabled == y.IsDisabled && x.Status == y.Status &&
                       x.CurrentTermEnd.Equals(y.CurrentTermEnd) && x.PlanQuantity == y.PlanQuantity;
            }

            public int GetHashCode(ChargebeeSubscription obj)
            {
                return HashCode.Combine(obj.MainEmail, obj.PlanId, obj.CustomerId, obj.SubscriptionId, obj.IsDisabled, (int)obj.Status, obj.CurrentTermEnd, obj.PlanQuantity);
            }
        }

        private SubscriptionRepository CreateRepository()
        {
            return new ServiceBuilder<SubscriptionRepository>()
               .SetFakeRepository(_fakeRepository)
               .Build();
        }
    }
}
