using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Application.Services;
using Ikiru.Parsnips.Domain.Chargebee;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.Functions.SearchFirmSubscriptionEventHandlers.Subscription;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ResourceNotFound;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Functions.SearchFirmSubscriptionEventHandlers.Invoice
{
    /// <summary>
    /// Determine if purchase token addon was added
    /// </summary>
    public class Generated
    {
        public class Payload : EventPayload
        {
        }

        public class Handler : IRequestHandler<Payload>
        {
            private readonly AddonRepository _addonRepository;
            private readonly ISearchFirmTokenProcessor _searchFirmTokenProcessor;
            private readonly SubscriptionRepository _subscriptionRepository;

            public Handler(AddonRepository addonRepository, ISearchFirmTokenProcessor searchFirmTokenProcessor, SubscriptionRepository subscriptionRepository)
            {
                _addonRepository = addonRepository;
                _searchFirmTokenProcessor = searchFirmTokenProcessor;
                _subscriptionRepository = subscriptionRepository;
            }

            public async Task<Unit> Handle(Payload request, CancellationToken cancellationToken)
            {
                var invoice = SubscriptionHelpers.GetInvoiceFromPayloadOrThrow(request);
                var subscriptions = await _subscriptionRepository.GetSubscriptionsByCustomerId(invoice.CustomerId);

                if (subscriptions?.Any() != true)
                    throw new ResourceNotFoundException(nameof(invoice.CustomerId), invoice.CustomerId);

                await AllocatePurchaseTokens(subscriptions.First().SearchFirmId, invoice);

                return Unit.Value;
            }

            private async Task AllocatePurchaseTokens(Guid searchFirmId, Domain.Chargebee.Invoice invoicePayload)
            {
                var reportedAddons = invoicePayload.LineItems?.Where(i => i.EntityType == EntityTypeEnum.Addon).ToArray() ?? new LineItem[0];
                var addonIds = reportedAddons.Select(a => a.EntityId).ToArray();

                var matchingPurchaseAddon = await _addonRepository.GetPurchaseTokenAddon(addonIds);
                if (matchingPurchaseAddon == null)
                    return;

                var allocateTokensAddon = reportedAddons.Single(a => a.EntityId == matchingPurchaseAddon.AddonId);

                await _searchFirmTokenProcessor.AddTokens(searchFirmId, TokenOriginType.Purchase, allocateTokensAddon.DateFrom, allocateTokensAddon.DateTo, allocateTokensAddon.Quantity);
            }
        }
    }
}
