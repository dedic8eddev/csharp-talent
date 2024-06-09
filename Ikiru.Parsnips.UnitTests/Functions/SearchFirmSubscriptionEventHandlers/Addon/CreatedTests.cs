using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Chargebee;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.Functions.SearchFirmSubscriptionEventHandlers.Addon;
using Ikiru.Parsnips.UnitTests.Helpers;
using Microsoft.Azure.Cosmos;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Functions.SearchFirmSubscriptionEventHandlers.Addon
{
    public class CreatedTests
    {
        private static AddonType s_NewAddonType = AddonType.PurchaseToken;
        private static AddonStatus s_NewAddonStatus = AddonStatus.Active;

        private readonly Created.Payload m_Request;

        private readonly FakeCosmos m_FakeCosmos;

        private readonly ChargebeeAddon m_MainChargebeeAddon;

        public CreatedTests()
        {
            var addonId = "subscription-token";
            m_Request = new Created.Payload
            {
                Value = new ChargebeeEventPayload
                {
                    Content = new Content
                    {
                        Addon = new Domain.Chargebee.Addon
                        {
                            Id = addonId,
                            Status = s_NewAddonStatus,
                            Period = 1,
                            PeriodUnit = PeriodUnitEnum.Month,
                            CurrencyCode = "EUR",
                            Price = 37,
                            Metadata = new AddonMetaData
                            {
                                Type = s_NewAddonType
                            }
                        }
                    }
                }
            };

            m_MainChargebeeAddon = new ChargebeeAddon { AddonId = addonId, AddonType = AddonType.PlanToken };
            var allAddons = new[]
                              {
                                  m_MainChargebeeAddon,
                                  new ChargebeeAddon { AddonId = "a-random-addon-id", AddonType = AddonType.PlanToken },
                                  new ChargebeeAddon { AddonId = "purchase-token", AddonType = AddonType.PurchaseToken },
                              };

            m_FakeCosmos = new FakeCosmos()
                          .EnableContainerLinqQuery(FakeCosmos.ChargebeeContainerName, ChargebeeAddon.PartitionKey, () => allAddons)
                          .EnableContainerReplace<ChargebeeAddon>(FakeCosmos.ChargebeeContainerName, m_MainChargebeeAddon.Id.ToString(), ChargebeeAddon.PartitionKey)
                          .EnableContainerInsert<ChargebeeAddon>(FakeCosmos.ChargebeeContainerName);
        }

        [Fact]
        public async Task CreatedInsertsAddonIfMissing()
        {
            var newAddonId = "new addon";

            // Given
            m_Request.Value.Content.Addon.Id = newAddonId;
            var handler = CreateHandler();

            // When
            await handler.Handle(m_Request, CancellationToken.None);

            // Then
            var container = m_FakeCosmos.ChargebeeContainer;

            container.Verify(c => c.CreateItemAsync(It.Is<ChargebeeAddon>(s =>
                                                                              s.Id != null && s.Id != Guid.Empty &&
                                                                              s.AddonId == newAddonId &&
                                                                              s.Period == m_Request.Value.Content.Addon.Period &&
                                                                              s.PeriodUnit == m_Request.Value.Content.Addon.PeriodUnit &&
                                                                              s.Status == m_Request.Value.Content.Addon.Status &&
                                                                              s.AddonType == m_Request.Value.Content.Addon.Metadata.Type &&
                                                                              s.CurrencyCode == m_Request.Value.Content.Addon.CurrencyCode &&
                                                                              s.Price == m_Request.Value.Content.Addon.Price
                                                                          ),
                                                     It.Is<PartitionKey>(p => p == new PartitionKey(ChargebeeAddon.PartitionKey)),
                                                     It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
        }

        public static IEnumerable<object[]> UpdateAddonTestData()
        {
            yield return new object[] { new Action<ChargebeeAddon>(a => a.Status = s_NewAddonStatus) };
            yield return new object[] { new Action<ChargebeeAddon>(a => a.AddonType = s_NewAddonType) };
            yield return new object[] { new Action<ChargebeeAddon>(_ => { }) };
        }

        [Theory]
        [MemberData(nameof(UpdateAddonTestData))]
        public async Task CreatedUpdatesAddonIfPresent(Action<ChargebeeAddon> existingAddonAction)
        {
            // Given
            existingAddonAction(m_MainChargebeeAddon);
            var handler = CreateHandler();

            // When
            await handler.Handle(m_Request, CancellationToken.None);

            // Then
            var container = m_FakeCosmos.ChargebeeContainer;

            container.Verify(c => c.ReplaceItemAsync(It.Is<ChargebeeAddon>(s =>
                                                                           s.Id == m_MainChargebeeAddon.Id &&
                                                                           s.AddonId == m_MainChargebeeAddon.AddonId &&
                                                                           s.Status == s_NewAddonStatus &&
                                                                           s.AddonType == s_NewAddonType &&
                                                                           s.Period == m_Request.Value.Content.Addon.Period &&
                                                                           s.PeriodUnit == m_Request.Value.Content.Addon.PeriodUnit &&
                                                                           s.CurrencyCode == m_Request.Value.Content.Addon.CurrencyCode &&
                                                                           s.Price == m_Request.Value.Content.Addon.Price),
                                                     It.Is<string>(id => id == m_MainChargebeeAddon.Id.ToString()),
                                                     It.Is<PartitionKey>(p => p == new PartitionKey(ChargebeeAddon.PartitionKey)),
                                                     It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task CreatedUpdatesTypeToUnknownIfMetadataNull()
        {
            // Given
            m_Request.Value.Content.Addon.Metadata = null;
            var handler = CreateHandler();

            // When
            await handler.Handle(m_Request, CancellationToken.None);

            // Then
            var container = m_FakeCosmos.ChargebeeContainer;

            container.Verify(c => c.ReplaceItemAsync(It.Is<ChargebeeAddon>(s =>
                                                                               s.AddonId == m_MainChargebeeAddon.AddonId &&
                                                                               s.AddonType == AddonType.Unknown
                                                                          ),
                                                     It.Is<string>(id => id == m_MainChargebeeAddon.Id.ToString()),
                                                     It.Is<PartitionKey>(p => p == new PartitionKey(ChargebeeAddon.PartitionKey)),
                                                     It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task CreatedInsertsWithTypeUnknownIfMetadataNull()
        {
            var newAddonId = "new addon";

            // Given
            m_Request.Value.Content.Addon.Id = newAddonId;
            m_Request.Value.Content.Addon.Metadata = null;
            var handler = CreateHandler();

            // When
            await handler.Handle(m_Request, CancellationToken.None);

            // Then
            var container = m_FakeCosmos.ChargebeeContainer;

            container.Verify(c => c.CreateItemAsync(It.Is<ChargebeeAddon>(s =>
                                                                              s.Id != null && s.Id != Guid.Empty &&
                                                                              s.AddonId == newAddonId &&
                                                                              s.AddonType == AddonType.Unknown
                                                                         ),
                                                    It.Is<PartitionKey>(p => p == new PartitionKey(ChargebeeAddon.PartitionKey)),
                                                    It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task CreatedThrowsIfAddonPayloadIsNull()
        {
            // Given
            m_Request.Value.Content.Addon = null;
            var handler = CreateHandler();

            // When
            var ex = await Record.ExceptionAsync(() => handler.Handle(m_Request, CancellationToken.None));

            // Then
            Assert.NotNull(ex);
            Assert.IsType<ArgumentNullException>(ex);
        }

        private Created.Handler CreateHandler() => new FunctionBuilder<Created.Handler>()
                                                    .SetFakeCosmos(m_FakeCosmos)
                                                    .Build();
    }
}
