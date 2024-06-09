using AutoMapper;
using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Application.Query.Subscription.Models;
using Ikiru.Parsnips.Application.Shared.Helpers;
using Ikiru.Parsnips.Application.Shared.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Application.Query.Subscription
{
    public class PlanQuery : IQueryHandler<PlanRequest, PlanResponse>
    {
        private readonly IMapper _mapper;
        private readonly SubscriptionRepository _subscriptionRepository;
        private readonly SearchFirmRepository _searchFirmRepository;
        private readonly IQueryHandler<EstimateRequest, EstimateResponse> _estimateQuery;


        public PlanQuery(
            SubscriptionRepository subscriptionRepository,
            SearchFirmRepository searchFirmRepository,
            IMapper mapper,
            IQueryHandler<EstimateRequest, EstimateResponse> estimateQuery)
        {
            _mapper = mapper;
            _subscriptionRepository = subscriptionRepository;
            _searchFirmRepository = searchFirmRepository;
            _estimateQuery = estimateQuery;
        }

        public async Task<PlanResponse> Handle(PlanRequest query)
        {
            if (query.Currency == null)
            {
                var searchFirm = await _searchFirmRepository.GetSearchFirmById(query.SearchFirmId);
                query.Currency = CurrencyHelper.GetCurrencyCodeFromCountryCode(searchFirm?.CountryCode) ?? "USD";
            }

            query.Coupons ??= new List<string>();

            var result = await _subscriptionRepository.GetPlans(query.Currency);

            if (result.Count == 0)
            {
                return new PlanResponse();
            }

            // Todo: the profile for this mapping is in Infrastructure.
            // We need to refactor as it is not referenced from this project and if we try to call it from another project that does not reference Infrastructure, we would fail
            var final = _mapper.Map<PlanResponse>(result);

            foreach (var plan in final)
            {
                var estimateRequest = new EstimateRequest()
                {
                    SubscriptionPlanId = plan.Id,
                    UnitQuantity = 1,
                    SubscriptionStartDate = DateTime.UtcNow,
                    Couponids = query.Coupons
                };
                var estimate = await _estimateQuery.Handle(estimateRequest);
                plan.Price = (Price)estimate;
            }
            return final;
        }

    }
}
