using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Chargebee;
using Ikiru.Parsnips.Functions.SearchFirmSubscriptionEventHandlers.Subscription;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure;
using Ikiru.Parsnips.UnitTests.Helpers;
using Microsoft.Azure.Cosmos;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Functions.SearchFirmSubscriptionEventHandlers.Subscription
{
    public class CancelledTests
    {
        private readonly Cancelled.Payload m_Request;

        private readonly FakeCosmos m_FakeCosmos;

        private SearchFirm m_SearchFirm;
        private readonly List<ChargebeeSubscription> m_AllSubscriptions;
        private readonly ChargebeeSubscription m_MainSubscription;
        private readonly Guid m_MainSubscriptionId;

        public CancelledTests()
        {
            m_SearchFirm = new SearchFirm
            {
                Name = "Test search firm",
                CountryCode = "GB",
                PhoneNumber = "0123456",
                RocketReachAttemptUseExpiredCredits = new List<DateTimeOffset> { DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(321) },
                IsEnabled = true
            };

            m_Request = new Cancelled.Payload
            {
                Value = new ChargebeeEventPayload
                {
                    Content = new Content
                    {
                        Subscription = new Domain.Chargebee.Subscription
                        {
                            Id = "abcd-subscription-id",
                            CustomerId = "efgh-customer-id",
                            PlanId = "ijkl-plan-id",
                            Status = Domain.Chargebee.Subscription.StatusEnum.NonRenewing,
                            CurrentTermEnd = DateTimeOffset.Now.AddSeconds(-1),
                            Metadata = new SubscriptionMetadata
                            {
                                SearchFirmId = m_SearchFirm.Id
                            }
                        }
                    }
                }
            };

            m_MainSubscription = new ChargebeeSubscription(m_SearchFirm.Id)
            {
                IsEnabled = true,
                SubscriptionId = m_Request.Value.Content.Subscription.Id,
                PlanId = m_Request.Value.Content.Subscription.PlanId,
                CustomerId = m_Request.Value.Content.Subscription.CustomerId,
                MainEmail = "main@subscription.email"
            };
            m_MainSubscriptionId = m_MainSubscription.Id;
            m_AllSubscriptions = new List<ChargebeeSubscription>
                                                                   {
                                                                       m_MainSubscription,
                                                                       new ChargebeeSubscription(m_SearchFirm.Id) { IsEnabled = false },
                                                                       new ChargebeeSubscription(m_SearchFirm.Id) { IsEnabled = false },
                                                                       new ChargebeeSubscription(Guid.NewGuid()) { IsEnabled = true },
                                                                       new ChargebeeSubscription(Guid.NewGuid()) { IsEnabled = false }
                                                                   };
            m_FakeCosmos = new FakeCosmos()
                          .EnableContainerLinqQuery(FakeCosmos.ChargebeeContainerName, m_SearchFirm.Id.ToString(), () => m_AllSubscriptions)
                          .EnableContainerReplace<SearchFirm>(FakeCosmos.SearchFirmsContainerName, m_SearchFirm.Id.ToString(), m_SearchFirm.Id.ToString())
                          .EnableContainerReplace<ChargebeeSubscription>(FakeCosmos.ChargebeeContainerName, m_MainSubscription.Id.ToString(), m_SearchFirm.Id.ToString())
                          .EnableContainerFetch(FakeCosmos.SearchFirmsContainerName, m_SearchFirm.Id.ToString(), m_SearchFirm.Id.ToString(), () => m_SearchFirm);
        }

        [Fact]
        public async Task CancelDisablesSearchFirm()
        {
            // Given
            var handler = CreateHandler();

            // When
            await handler.Handle(m_Request, CancellationToken.None);

            // Then
            var container = m_FakeCosmos.SearchFirmsContainer;

            container.Verify(c => c.ReplaceItemAsync(It.Is<SearchFirm>(s =>
                                                                           s.Id == m_SearchFirm.Id &&
                                                                           s.Name == m_SearchFirm.Name &&
                                                                           s.CountryCode == m_SearchFirm.CountryCode &&
                                                                           s.PhoneNumber == m_SearchFirm.PhoneNumber &&
                                                                           s.RocketReachAttemptUseExpiredCredits.Count == m_SearchFirm.RocketReachAttemptUseExpiredCredits.Count &&
                                                                           s.RocketReachAttemptUseExpiredCredits.All(cr => m_SearchFirm.RocketReachAttemptUseExpiredCredits.Any(join => cr == join)) &&
                                                                           s.IsEnabled == false
                                                                  ),
                                                     It.Is<string>(id => id == m_SearchFirm.Id.ToString()),
                                                     It.Is<PartitionKey>(p => p == new PartitionKey(m_SearchFirm.Id.ToString())),
                                                     It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task CancelDoesNotDisableSearchFirmWhenAnotherActiveSubscriptionPresent()
        {
            // Given
            m_AllSubscriptions.Add(new ChargebeeSubscription(m_SearchFirm.Id) { IsEnabled = true, SubscriptionId = Guid.NewGuid().ToString() });
            var handler = CreateHandler();

            // When
            await handler.Handle(m_Request, CancellationToken.None);

            // Then
            var container = m_FakeCosmos.SearchFirmsContainer;

            container.Verify(c => c.ReplaceItemAsync(It.IsAny<SearchFirm>(),
                                                     It.IsAny<string>(),
                                                     It.IsAny<PartitionKey>(),
                                                     It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CancelDisablesLocallyStoredSubscription()
        {
            // Given
            var handler = CreateHandler();

            // When
            await handler.Handle(m_Request, CancellationToken.None);

            // Then
            var container = m_FakeCosmos.ChargebeeContainer;

            container.Verify(c => c.ReplaceItemAsync(It.Is<ChargebeeSubscription>(s =>
                                                                                      s.Discriminator == "ChargebeeSubscription" &&
                                                                                      s.Id == m_MainSubscriptionId &&
                                                                                      s.PlanId == m_Request.Value.Content.Subscription.PlanId &&
                                                                                      s.CustomerId == m_Request.Value.Content.Subscription.CustomerId &&
                                                                                      s.SearchFirmId == m_SearchFirm.Id &&
                                                                                      s.SubscriptionId == m_Request.Value.Content.Subscription.Id &&
                                                                                      s.IsEnabled == false &&
                                                                                      s.Status == m_Request.Value.Content.Subscription.Status &&
                                                                                      s.CurrentTermEnd == m_Request.Value.Content.Subscription.CurrentTermEnd
                                                                                 ),
                                                     It.Is<string>(id => id == m_MainSubscription.Id.ToString()),
                                                     It.Is<PartitionKey>(p => p == new PartitionKey(m_SearchFirm.Id.ToString())),
                                                     It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task CancelThrowsIfPayloadIsNull()
        {
            // Given
            m_Request.Value.Content.Subscription = null;
            var handler = CreateHandler();

            // When
            var ex = await Record.ExceptionAsync(() => handler.Handle(m_Request, CancellationToken.None));

            // Then
            Assert.NotNull(ex);
            Assert.IsType<ArgumentNullException>(ex);
        }

        [Fact]
        public async Task CancelThrowsIfMetadataIsNull()
        {
            // Given
            m_Request.Value.Content.Subscription.Metadata = null;
            var handler = CreateHandler();

            // When
            var ex = await Record.ExceptionAsync(() => handler.Handle(m_Request, CancellationToken.None));

            // Then
            Assert.NotNull(ex);
            Assert.IsType<ArgumentNullException>(ex);
        }

        [Fact]
        public async Task CancelThrowsIfPayloadMetadataSearchFirmIdIsEmpty()
        {
            // Given
            m_Request.Value.Content.Subscription.Metadata.SearchFirmId = Guid.Empty;
            var handler = CreateHandler();

            // When
            var ex = await Record.ExceptionAsync(() => handler.Handle(m_Request, CancellationToken.None));

            // Then
            Assert.NotNull(ex);
            Assert.IsType<ArgumentException>(ex);
        }

        [Fact]
        public async Task CancelThrowsIfSearchFirmIsNotFound()
        {
            // Given
            m_SearchFirm = null;
            var handler = CreateHandler();

            // When
            var ex = await Record.ExceptionAsync(() => handler.Handle(m_Request, CancellationToken.None));

            // Then
            Assert.NotNull(ex);
            Assert.IsType<ParamValidationFailureException>(ex);
        }

        private Cancelled.Handler CreateHandler() => new FunctionBuilder<Cancelled.Handler>()
                                                    .SetFakeCosmos(m_FakeCosmos)
                                                    .Build();
    }
}
