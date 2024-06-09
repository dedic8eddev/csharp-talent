using AutoMapper;
using Ikiru.Parsnips.Api.Authentication;
using Ikiru.Parsnips.Api.Controllers.Subscription.Models;
using Ikiru.Parsnips.Api.Security;
using Ikiru.Parsnips.Application.Command;
using Ikiru.Parsnips.Application.Command.Subscription.Models;
using Ikiru.Parsnips.Application.Infrastructure.Subscription;
using Ikiru.Parsnips.Application.Query;
using Ikiru.Parsnips.Application.Query.Subscription.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Subscription
{
    [AllowInactiveSubscriptions]
    [Authorize(Policy = TeamMemberRequirement.POLICY)]
    [ApiController]
    [Route("/api/[controller]")]
    public class SubscriptionController : ControllerBase
    {
        private readonly IQueryHandler<PlanRequest, PlanResponse> _planQuery;
        private readonly ICommandHandler<CreatePaymentIntentRequest, CreatePaymentIntentResponse> _createPaymentIntentCommand;
        private readonly ICommandHandler<UpdateAllPlansRequest, UpdateAllPlansResponse> _updateAllPlansCommand;
        private readonly ICommandHandler<UpdateAllAddonsRequest, UpdateAllAddonsResponse> _updateAllAddonsCommand;
        private readonly ICommandHandler<UpdateAllCouponsRequest, UpdateAllCouponsResponse> _updateAllCouponsCommand;
        private readonly IQueryHandler<EstimateRequest, EstimateResponse> _estimateQuery;
        private readonly ICommandHandler<CreateSubscriptionRequest, CreateSubscriptionResponse> _createSubscriptionCommand;
        private readonly CurrentSubscriptionDetails _currentSubscriptionDetails;
        private readonly IMapper _mapper;

        private readonly AuthenticatedUserAccessor _authenticatedUserAccessor;

        public SubscriptionController(IQueryHandler<PlanRequest, PlanResponse> planQuery,
                                      AuthenticatedUserAccessor authenticatedUserAccessor,
                                      IQueryHandler<EstimateRequest, EstimateResponse> estimateQuery,
                                      ICommandHandler<CreatePaymentIntentRequest, CreatePaymentIntentResponse> createPaymentIntentCommand,
                                      ICommandHandler<CreateSubscriptionRequest, CreateSubscriptionResponse> createSubscriptionCommand,
                                      ICommandHandler<UpdateAllCouponsRequest, UpdateAllCouponsResponse> updateAllCouponsCommand,
                                      ICommandHandler<UpdateAllAddonsRequest, UpdateAllAddonsResponse> updateAllAddonsCommand,
                                      ICommandHandler<UpdateAllPlansRequest, UpdateAllPlansResponse> updateAllPlansCommand,
                                      CurrentSubscriptionDetails currentSubscriptionDetails,
                                      IMapper mapper)
        {
            _planQuery = planQuery;
            _estimateQuery = estimateQuery;
            _authenticatedUserAccessor = authenticatedUserAccessor;
            _createPaymentIntentCommand = createPaymentIntentCommand;
            _updateAllPlansCommand = updateAllPlansCommand;
            _updateAllAddonsCommand = updateAllAddonsCommand;
            _updateAllCouponsCommand = updateAllCouponsCommand;
            _createSubscriptionCommand = createSubscriptionCommand;
            _currentSubscriptionDetails = currentSubscriptionDetails;
            _mapper = mapper;
        }

        [HttpPost("plans")]
        [Consumes("application/json")]
        public async Task<IActionResult> Plans(PlansRequest plansRequest = null)
        {
            var authenticatedUser = _authenticatedUserAccessor.GetAuthenticatedUser();
            var query = new PlanRequest()
            {
                SearchFirmId = authenticatedUser.SearchFirmId,
                Currency = plansRequest?.Currency,
                Coupons = plansRequest?.Coupons
            };
            var result = await _planQuery.Handle(query);
            return Ok(result);
        }

        [HttpPost("[action]")]
        [Consumes("application/json")]
        public async Task<IActionResult> GetEstimate(EstimateRequest estimateRequest)
        {
            var response = await _estimateQuery.Handle(estimateRequest);

            if (response.GeneralException)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPost("[action]")]
        [Consumes("application/json")]
        public async Task<IActionResult> CreatePaymentIntent(CreatePaymentIntent createPaymentIntent)
        {
            var authenticatedUser = _authenticatedUserAccessor.GetAuthenticatedUser();

            var createPaymentIntentRequest = _mapper.Map<CreatePaymentIntentRequest>(createPaymentIntent);

            createPaymentIntentRequest.SearchFirmId = authenticatedUser.SearchFirmId;

            var responsePaymentIntent = await _createPaymentIntentCommand.Handle(createPaymentIntentRequest);

            if (responsePaymentIntent.GeneralException)
            {
                return BadRequest(responsePaymentIntent);
            }

            return Ok(responsePaymentIntent);
        }

        [HttpPost("[action]")]
        [Consumes("application/json")]
        public async Task<IActionResult> SynchronizeChargebeeProductCatalogue()
        {
            var planCount = (await _updateAllPlansCommand.Handle(new UpdateAllPlansRequest())).Updated;
            var addonResult = await _updateAllAddonsCommand.Handle(new UpdateAllAddonsRequest());
            var couponCount = (await _updateAllCouponsCommand.Handle(new UpdateAllCouponsRequest())).Updated;
            return Ok(new { updatedPlans = planCount, updatedAddons = addonResult, updatedCoupons = couponCount });
        }

        [HttpPost("[action]")]
        [Consumes("application/json")]
        public async Task<IActionResult> CreateSubscription(CreateSubscription createSubscription)
        {
            var createSubscriptionRequest = new CreateSubscriptionRequest
            {
                SearchFirmId = _authenticatedUserAccessor.GetAuthenticatedUser().SearchFirmId,
                SubscriptionPlanId = createSubscription.SubscriptionPlanId,
                PaymentIntentId = createSubscription.PaymentIntentId,
                CouponIds = createSubscription.CouponIds,
                UnitQuantity = createSubscription.UnitQuantity
            };

            await _createSubscriptionCommand.Handle(createSubscriptionRequest);

            return Ok();

        }

        [HttpGet]
        public async Task<IActionResult> GetCurrent()
        {
            var user = _authenticatedUserAccessor.GetAuthenticatedUser();
            var result = await _currentSubscriptionDetails.Get(user.SearchFirmId);
            return await Task.FromResult(Ok(result));
        }
    }
}
