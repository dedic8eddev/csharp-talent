using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Chargebee;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.Functions.SearchFirmSubscriptionEventHandlers.Subscription;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure;
using Ikiru.Parsnips.UnitTests.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Functions.SearchFirmSubscriptionEventHandlers.Subscription
{
    public class CreatedTests
    {
        private const string _PLAN_ID = "ijkl-plan-id";
        private const string _SEARCH_FIRM_NAME = "Test search firm";
        private const string _SEARCH_FIRM_COUNTRY_CODE = "GB";
        private const string _SEARCH_FIRM_PHONE_NUMBER = "0123456";

        private readonly Created.Payload _request;

        private readonly FakeRepository _fakeRepository = new FakeRepository();

        private Guid _searchFirmId;

        private readonly SearchFirm _searchFirm;
        private readonly Guid _mainSubscriptionId;
        private readonly ChargebeeSubscription _mainSubscription;
        private readonly SubscriptionAddon _mainAddon;
        private readonly ChargebeeAddon _mainChargebeeAddon;
        private readonly ChargebeePlan _chargebeePlan;

        public CreatedTests()
        {
            _searchFirm = new SearchFirm
                          {
                              Name = _SEARCH_FIRM_NAME,
                              CountryCode = _SEARCH_FIRM_COUNTRY_CODE,
                              PhoneNumber = _SEARCH_FIRM_PHONE_NUMBER,
                              RocketReachAttemptUseExpiredCredits = new List<DateTimeOffset> { DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(321) },
                              IsEnabled = true
                          };
            _searchFirmId = _searchFirm.Id;

            var customerId = "efgh-customer-id";
            var addonId = "subscription-tokens";
            _mainAddon = new SubscriptionAddon { Id = addonId, Quantity = 3 };
            _request = new Created.Payload
            {
                Value = new ChargebeeEventPayload
                {
                    Content = new Content
                    {
                        Subscription = new Domain.Chargebee.Subscription
                        {
                            Id = "abcd-subscription-id",
                            CustomerId = customerId,
                            PlanId = _PLAN_ID,
                            Addons = new List<SubscriptionAddon>
                                     {
                                         _mainAddon,
                                         new SubscriptionAddon { Id = "other-addon", Quantity = 50 }
                                     },
                            Metadata = new SubscriptionMetadata
                            {
                                SearchFirmId = _searchFirm.Id
                            },
                            CurrentTermEnd = DateTimeOffset.Now.AddDays(2),
                            Status = Domain.Chargebee.Subscription.StatusEnum.NonRenewing,
                            PlanQuantity = 7
                        },
                        Customer = new Customer
                        {
                            Id = "another customer id which should never happen in real life",
                            Email = "another@email.com",
                            Metadata = new CustomerMetadata
                            {
                                SearchFirmId = _searchFirm.Id
                            }
                        }
                    }
                }
            };

            _mainSubscription = new ChargebeeSubscription(_searchFirm.Id)
            {
                IsEnabled = false,
                SubscriptionId = _request.Value.Content.Subscription.Id,
                PlanId = _PLAN_ID,
                CustomerId = customerId,
                MainEmail = "main@subscription.email",
                CurrentTermEnd = DateTimeOffset.Now.AddDays(3),
                Status = Domain.Chargebee.Subscription.StatusEnum.Active
            };
            _mainSubscriptionId = _mainSubscription.Id;
            var allSubscriptions = new List<ChargebeeSubscription>
                                    {
                                        _mainSubscription,
                                        new ChargebeeSubscription(_searchFirm.Id) { IsEnabled = false },
                                        new ChargebeeSubscription(_searchFirm.Id) { IsEnabled = false },
                                        new ChargebeeSubscription(Guid.NewGuid()) { IsEnabled = true },
                                        new ChargebeeSubscription(Guid.NewGuid()) { IsEnabled = false }
                                    };

            _mainChargebeeAddon = new ChargebeeAddon { AddonId = addonId, AddonType = AddonType.PlanToken, Status = AddonStatus.Active };

            _chargebeePlan = new ChargebeePlan { PlanId = _PLAN_ID, Status = PlanStatus.Active, PeriodUnit = PeriodUnitEnum.Month };
            _fakeRepository.AddToRepository(_searchFirm,
                                            _mainChargebeeAddon,
                                            new ChargebeeAddon { AddonId = "a-random-addon-id", AddonType = AddonType.PlanToken, Status = AddonStatus.Active },
                                            new ChargebeeAddon { AddonId = "purchase-token", AddonType = AddonType.PurchaseToken, Status = AddonStatus.Active },
                                            _chargebeePlan);

            _fakeRepository.AddToRepository(allSubscriptions.ToArray<object>());
        }

        [Fact]
        public async Task CreatedEnablesSearchFirm()
        {
            // Given
            _searchFirm.IsEnabled = false;
            var handler = CreateHandler();

            // When
            await handler.Handle(_request, CancellationToken.None);

            // Then
            var searchFirm = await _fakeRepository.GetItem<SearchFirm>(_searchFirmId.ToString(), _searchFirmId.ToString());
            Assert.Equal(_SEARCH_FIRM_NAME, searchFirm.Name);
            Assert.Equal(_SEARCH_FIRM_COUNTRY_CODE, searchFirm.CountryCode);
            Assert.Equal(_SEARCH_FIRM_PHONE_NUMBER, searchFirm.PhoneNumber);
            Assert.Equal(_searchFirm.RocketReachAttemptUseExpiredCredits.Count, searchFirm.RocketReachAttemptUseExpiredCredits.Count);
            Assert.True(searchFirm.RocketReachAttemptUseExpiredCredits.All(cr => _searchFirm.RocketReachAttemptUseExpiredCredits.Any(join => cr == join)));
            Assert.True(searchFirm.IsEnabled);
        }

        [Theory, CombinatorialData]
        public async Task CreatedUpsertsEnabledLocallyStoredSubscription(bool subscriptionPresent)
        {
            // Given
            if (!subscriptionPresent)
                await _fakeRepository.Delete(_mainSubscription);

            var handler = CreateHandler();

            // When
            await handler.Handle(_request, CancellationToken.None);

            // Then
            var subscription = await _fakeRepository.GetByQuery<ChargebeeSubscription, ChargebeeSubscription>(_searchFirmId.ToString(),
                i => i.Where(s =>
                    (s.Id != null && s.Id != Guid.Empty && !subscriptionPresent || s.Id == _mainSubscriptionId) &&
                    s.SearchFirmId == _request.Value.Content.Subscription.Metadata.SearchFirmId &&
                    s.MainEmail == _request.Value.Content.Customer.Email &&
                    s.PlanId == _request.Value.Content.Subscription.PlanId &&
                    s.CustomerId == _request.Value.Content.Subscription.CustomerId &&
                    s.SubscriptionId == _request.Value.Content.Subscription.Id &&
                    s.IsEnabled &&
                    s.CurrentTermEnd == _request.Value.Content.Subscription.CurrentTermEnd &&
                    s.Status == _request.Value.Content.Subscription.Status &&
                    s.PlanQuantity == _request.Value.Content.Subscription.PlanQuantity));

            Assert.Single(subscription);
        }

        [Theory]
        [InlineData(PeriodUnitEnum.Month)]
        [InlineData(PeriodUnitEnum.Year)]
        public async Task CreatedAllocatesTokens(PeriodUnitEnum periodUnit)
        {
            // Given
            _chargebeePlan.PeriodUnit = periodUnit;
            var numberOfMonth = periodUnit == PeriodUnitEnum.Month ? 1 : 12;
            var handler = CreateHandler();

            // When
            await handler.Handle(_request, CancellationToken.None);

            // Then
            var allTokens = await _fakeRepository.GetByQuery<SearchFirmToken, SearchFirmToken>(_searchFirmId.ToString(),
                i => i.Where(s =>
                    s.SearchFirmId == _searchFirm.Id &&
                    s.OriginType == TokenOriginType.Plan &&
                    !s.IsSpent &&
                    s.SpentAt == null &&
                    s.SpentByUserId == null));
            Assert.Equal(_mainAddon.Quantity * numberOfMonth, allTokens.Count);

            for (var month = 1; month <= numberOfMonth; ++month)
            {
                var validFromDate = DateTimeOffset.UtcNow.AddMonths(month - 1).UtcDateTime.Date;
                var expiredDate = validFromDate.AddMonths(1).AddDays(1);

                var tokens = await _fakeRepository.GetByQuery<SearchFirmToken, SearchFirmToken>(_searchFirmId.ToString(),
                    i => i.Where(s =>
                        s.Id != null && s.Id != Guid.Empty &&
                        s.SearchFirmId == _searchFirm.Id &&
                        s.OriginType == TokenOriginType.Plan &&
                        s.ExpiredAt.Date == expiredDate &&
                        s.ValidFrom == validFromDate &&
                        !s.IsSpent &&
                        s.SpentAt == null &&
                        s.SpentByUserId == null));
                Assert.True(_mainAddon.Quantity == tokens.Count, $"Month '{month}/{numberOfMonth}', expected {_mainAddon.Quantity} tokens, allocated {tokens.Count} tokens.");
            }
        }

        [Theory, CombinatorialData]
        public async Task CreatedDoesNotAllocateTokensIfNoMatchingPlanTokenAddons(bool matchingId)
        {
            // Given
            if (matchingId)
                _mainChargebeeAddon.AddonType = AddonType.PurchaseToken;
            else
                _mainChargebeeAddon.AddonId = "another-plan-token-id";

            var handler = CreateHandler();

            // When
            await handler.Handle(_request, CancellationToken.None);

            // Then
            var tokens = await _fakeRepository.GetByQuery<SearchFirmToken, SearchFirmToken>(_searchFirmId.ToString(),
                i => i.Where(s => s.OriginType == TokenOriginType.Plan));

            Assert.Empty(tokens);
        }

        [Fact]
        public async Task CreatedThrowsIfCustomerPayloadIsEmpty()
        {
            // Given
            _request.Value.Content.Customer = null;
            var handler = CreateHandler();

            // When
            var ex = await Record.ExceptionAsync(() => handler.Handle(_request, CancellationToken.None));

            // Then
            Assert.NotNull(ex);
            Assert.IsType<ArgumentNullException>(ex);
        }

        [Fact]
        public async Task CreatedThrowsIfPayloadIsNull()
        {
            // Given
            _request.Value.Content.Subscription = null;
            var handler = CreateHandler();

            // When
            var ex = await Record.ExceptionAsync(() => handler.Handle(_request, CancellationToken.None));

            // Then
            Assert.NotNull(ex);
            Assert.IsType<ArgumentNullException>(ex);
        }

        [Fact]
        public async Task CreatedThrowsIfMetadataIsNull()
        {
            // Given
            _request.Value.Content.Subscription.Metadata = null;
            var handler = CreateHandler();

            // When
            var ex = await Record.ExceptionAsync(() => handler.Handle(_request, CancellationToken.None));

            // Then
            Assert.NotNull(ex);
            Assert.IsType<ArgumentNullException>(ex);
        }

        [Fact]
        public async Task CreatedThrowsIfPayloadMetadataSearchFirmIdIsEmpty()
        {
            // Given
            _request.Value.Content.Subscription.Metadata.SearchFirmId = Guid.Empty;
            var handler = CreateHandler();

            // When
            var ex = await Record.ExceptionAsync(() => handler.Handle(_request, CancellationToken.None));

            // Then
            Assert.NotNull(ex);
            Assert.IsType<ArgumentException>(ex);
        }

        [Fact]
        public async Task CreatedThrowsIfSearchFirmIsNotFound()
        {
            // Given
            _searchFirm.Id = Guid.NewGuid();
            var handler = CreateHandler();

            // When
            var ex = await Record.ExceptionAsync(() => handler.Handle(_request, CancellationToken.None));

            // Then
            Assert.NotNull(ex);
            Assert.IsType<ParamValidationFailureException>(ex);
        }

        private Created.Handler CreateHandler() => new FunctionBuilder<Created.Handler>()
                                                    .SetFakeRepository(_fakeRepository)
                                                    .Build();
    }
}
