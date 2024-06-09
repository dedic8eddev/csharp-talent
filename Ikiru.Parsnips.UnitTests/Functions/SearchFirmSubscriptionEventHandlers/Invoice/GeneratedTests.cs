using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Chargebee;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.Functions.SearchFirmSubscriptionEventHandlers.Invoice;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ResourceNotFound;
using Ikiru.Parsnips.UnitTests.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Functions.SearchFirmSubscriptionEventHandlers.Invoice
{
    public class GeneratedTests
    {
        private readonly FakeRepository _fakeRepository = new FakeRepository();

        private readonly SearchFirm _searchFirm = new SearchFirm();

        private readonly Generated.Payload _request;
        private readonly string _purchaseTokenId = "rocketreach-token-id";
        private readonly string _customerId = "id-of-customer-buying-token";
        private readonly string _subscriptionId = "id-of-subscription-for-customer-buying-token";
        private readonly string _monthlySubscriptionTokenId = "monthly-subscription-token";
        private readonly int _numberOfTokens = 37;
        private readonly DateTimeOffset _validFrom = DateTimeOffset.Now.AddDays(5);
        private readonly DateTimeOffset _validTo = DateTimeOffset.Now.AddDays(10);

        public GeneratedTests()
        {
            _request = GenerateTestRequestPayload();

            var purchaseTokeAddon = new ChargebeeAddon
            {
                AddonId = _purchaseTokenId,
                AddonType = AddonType.PurchaseToken,
                Status = AddonStatus.Active
            };

            var subscription = new ChargebeeSubscription(_searchFirm.Id)
            {
                SubscriptionId = _subscriptionId,
                CustomerId = _customerId
            };
            var otherSubscription = new ChargebeeSubscription(_searchFirm.Id)
            {
                SubscriptionId = "abcdef-subscription-id",
                CustomerId = _customerId
            };            
            _fakeRepository.AddToRepository(purchaseTokeAddon, subscription, otherSubscription);
        }

        [Fact]
        public async Task GeneratedThrowsWhenNoInvoice()
        {
            // Given
            var payload = new Generated.Payload { Value = new ChargebeeEventPayload { Content = new Content() } };
            var handler = CreateHandler();

            // When
            var ex = await Record.ExceptionAsync(() => handler.Handle(payload, CancellationToken.None));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentNullException>(ex);
        }

        [Fact]
        public async Task GeneratedThrowsWhenNoSubscription()
        {
            // Given
            var payload = new Generated.Payload { Value = new ChargebeeEventPayload { Content = new Content { Invoice = new Domain.Chargebee.Invoice { CustomerId = "missing-customer-id"} } } };
            var handler = CreateHandler();

            // When
            var ex = await Record.ExceptionAsync(() => handler.Handle(payload, CancellationToken.None));

            Assert.NotNull(ex);
            Assert.IsType<ResourceNotFoundException>(ex);
        }

        [Fact]
        public async Task GeneratedAllocatesTokens()
        {
            // Given
            var handler = CreateHandler();

            // When
            await handler.Handle(_request, CancellationToken.None);

            // Then
            var allTokens = await _fakeRepository.GetByQuery<SearchFirmToken, SearchFirmToken>(_searchFirm.Id.ToString(),
                                                                                               i => i.Where(s =>
                                                                                                                s.SearchFirmId == _searchFirm.Id &&
                                                                                                                s.OriginType == TokenOriginType.Purchase &&
                                                                                                                s.ValidFrom == _validFrom.UtcDateTime.Date &&
                                                                                                                s.ExpiredAt == _validTo &&
                                                                                                                !s.IsSpent &&
                                                                                                                s.SpentAt == null &&
                                                                                                                s.SpentByUserId == null));
            Assert.Equal(_numberOfTokens, allTokens.Count);
        }

        [Theory, CombinatorialData]
        public async Task DoesNothingWhenNoAddons(bool isEmptyCollection)
        {
            // Given
            _request.Value.Content.Invoice.LineItems = isEmptyCollection ? new List<LineItem>() : null;
            var handler = CreateHandler();

            // When
            await handler.Handle(_request, CancellationToken.None);

            // Then
            var allTokens = await _fakeRepository.GetByQuery<SearchFirmToken>(_ => true);
            Assert.Empty(allTokens);
        }

        private Generated.Payload GenerateTestRequestPayload()
        {
            return new Generated.Payload
            {
                Value = new ChargebeeEventPayload
                {
                    Content = new Content
                    {
                        Invoice = new Domain.Chargebee.Invoice
                        {
                            CustomerId = _customerId,
                            Recurring = false,
                            Status = StatusEnum.Paid,
                            Deleted = false,
                            LineItems = new List<LineItem>
                            {
                                new LineItem
                                {
                                    Quantity = 41,
                                    CustomerId = _customerId,
                                    EntityType = EntityTypeEnum.Addon,
                                    EntityId = _monthlySubscriptionTokenId,
                                    DateFrom = _validFrom,
                                    DateTo = _validTo
                                },
                                new LineItem
                                {
                                    Quantity = _numberOfTokens,
                                    CustomerId = _customerId,
                                    EntityType = EntityTypeEnum.Addon,
                                    EntityId = _purchaseTokenId,
                                    DateFrom = _validFrom,
                                    DateTo = _validTo
                                }
                            }
                        }
                    }
                }
            };
        }

        private Generated.Handler CreateHandler() => new FunctionBuilder<Generated.Handler>()
                                                    .SetFakeRepository(_fakeRepository)
                                                    .Build();

    }
}
