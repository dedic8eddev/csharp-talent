using Ikiru.Parsnips.Api.Authentication;
using Ikiru.Parsnips.Application.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Ikiru.Parsnips.Api.Security
{
    public class ActiveSubscriptionPresentAuthorizeFilter : IAsyncAuthorizationFilter
    {
        private readonly AuthenticatedUserAccessor _authenticatedUserAccessor;
        private readonly SubscriptionRepository _subscriptionRepository;
        private readonly ILogger<ActiveSubscriptionPresentAuthorizeFilter> _logger;

        public ActiveSubscriptionPresentAuthorizeFilter(AuthenticatedUserAccessor authenticatedUserAccessor, SubscriptionRepository subscriptionRepository
                                                        , ILogger<ActiveSubscriptionPresentAuthorizeFilter> logger)
        {
            _authenticatedUserAccessor = authenticatedUserAccessor;
            _subscriptionRepository = subscriptionRepository;
            _logger = logger;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (!IsContextValid(context))
                return;

            if (RootAllowsAnonymousAccess(context))
                return;

            if (RootAllowsInactiveSubscription(context))
                return;

            var authenticatedUser = _authenticatedUserAccessor.TryGetAuthenticatedUser();

            if (authenticatedUser == null)
                return;

            if (await SubscriptionActive(authenticatedUser))
                return;

            var problemDetails = new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.2",
                Status = StatusCodes.Status402PaymentRequired,
                Title = "There is no active subscription present",
                Extensions = { { "errors", new Dictionary<string, bool> { { "subscriptionEnabled", false } } } }
            };

            context.Result = new ObjectResult(problemDetails)
            {
                ContentTypes = new MediaTypeCollection { new MediaTypeHeaderValue("application/json") },
                StatusCode = StatusCodes.Status402PaymentRequired
            };
        }

        private bool RootAllowsAnonymousAccess(AuthorizationFilterContext context)
            => context.Filters.Any(item => item is IAllowAnonymousFilter)
                || context.ActionDescriptor.EndpointMetadata.Any(d => d.GetType() == typeof(AllowAnonymousAttribute));

        private bool RootAllowsInactiveSubscription(AuthorizationFilterContext context)
            => context.ActionDescriptor.EndpointMetadata.Any(d => d.GetType() == typeof(AllowInactiveSubscriptionsAttribute));

        /// <summary>
        /// I am not sure this validation is required, we will analyze logs later and see if we have any errors from here.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private bool IsContextValid(AuthorizationFilterContext context)
        {
            if (context == null)
            {
                _logger.LogInformation("Context is null when calling OnAuthorizationAsync");
                return false;
            }

            if (context.ActionDescriptor != null)
                return true;

            var errorProblemDetails = new ProblemDetails
                                      {
                                          Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                                          Status = StatusCodes.Status400BadRequest,
                                          Title = "Unspecified error",
                                          Extensions = { { "errors", new Dictionary<string, string[]> { { "parameters", new[] { nameof(context.ActionDescriptor) } } } } }
                                      };

            context.Result = new BadRequestObjectResult(errorProblemDetails);
            
            _logger.LogInformation("Context.ActionDescriptor is null when calling OnAuthorizationAsync");
            
            return false;
        }

        private async Task<bool> SubscriptionActive(AuthenticatedUser authenticatedUser)
        {
            var subscriptions = await _subscriptionRepository.GetActiveSubscriptionsForSearchFirm(authenticatedUser.SearchFirmId);
            return subscriptions.Any();
        }
    }
}
