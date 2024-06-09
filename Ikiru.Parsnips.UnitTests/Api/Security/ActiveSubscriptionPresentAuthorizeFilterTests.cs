using Ikiru.Parsnips.Api.Security;
using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.UnitTests.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ikiru.Parsnips.Domain;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Security
{
    public class ActiveSubscriptionPresentAuthorizeFilterTests
    {
        private AuthorizationFilterContext _context;
        private readonly IList<IFilterMetadata> _filters = new List<IFilterMetadata>();
        private readonly List<object> _endpointMetadata = new List<object>();
        private Guid? _searchFirmId = Guid.NewGuid();
        private Guid? _userId = Guid.NewGuid();
        private readonly FakeRepository _fakeRepository = new FakeRepository();
        private ChargebeeSubscription _subscription;

        public ActiveSubscriptionPresentAuthorizeFilterTests()
        {
            _subscription = new ChargebeeSubscription(_searchFirmId.Value)
            {
                IsEnabled = true,
                CurrentTermEnd = DateTimeOffset.UtcNow.AddDays(1),
                Status = Domain.Chargebee.Subscription.StatusEnum.Active
            };

            _fakeRepository.Add(_subscription);
        }

        [Fact]
        public async Task DoesNothingWhenContextNotSet()
        {
            // Arrange
            var filter = CreateAutorizeFilter();

            // Act
            await filter.OnAuthorizationAsync(null);

            // Assert
            Assert.Null(_context.Result);
        }

        [Theory, CombinatorialData]
        public async Task DoesNothingWhenAnonymous(bool setFilter)
        {
            // Arrange
            if (setFilter)
                _filters.Add(Mock.Of<IAllowAnonymousFilter>());
            else
                _endpointMetadata.Add(new AllowAnonymousAttribute());

            var filter = CreateAutorizeFilter();

            // Act
            await filter.OnAuthorizationAsync(_context);

            // Assert
            Assert.Null(_context.Result);
        }

        [Fact]
        public async Task DoesNothingWhenAllowsInactiveSubscription()
        {
            // Arrange
            _endpointMetadata.Add(new AllowInactiveSubscriptionsAttribute());
            var filter = CreateAutorizeFilter();

            // Act
            await filter.OnAuthorizationAsync(_context);

            // Assert
            Assert.Null(_context.Result);
        }

        [Fact]
        public async Task DoesNothingWhenNoAuthenticatedUser()
        {
            // Arrange
            _searchFirmId = null;
            var filter = CreateAutorizeFilter();

            // Act
            await filter.OnAuthorizationAsync(_context);

            // Assert
            Assert.Null(_context.Result);
        }

        [Fact]
        public async Task DoesNothingWhenActiveSubscriptionIsPresent()
        {
            // Arrange
            var filter = CreateAutorizeFilter();

            // Act
            await filter.OnAuthorizationAsync(_context);

            // Assert
            Assert.Null(_context.Result);
        }

        public static IEnumerable<object[]> NoSubscriptionTestData()
        {
            yield return new[] { new Action<ChargebeeSubscription>(s => s.IsEnabled = false) };
            yield return new[] { new Action<ChargebeeSubscription>(s => s.CurrentTermEnd = DateTimeOffset.UtcNow.AddDays(-1)) };
            yield return new[] { new Action<ChargebeeSubscription>(s => s.Status = Domain.Chargebee.Subscription.StatusEnum.UnKnown) };
            yield return new[] { new Action<ChargebeeSubscription>(s => s.Status = Domain.Chargebee.Subscription.StatusEnum.Future) };
            yield return new[] { new Action<ChargebeeSubscription>(s => s.Status = Domain.Chargebee.Subscription.StatusEnum.Paused) };
            yield return new[] { new Action<ChargebeeSubscription>(s => s.Status = Domain.Chargebee.Subscription.StatusEnum.Cancelled) };
        }

        [Theory]
        [MemberData(nameof(NoSubscriptionTestData))]
        public async Task Returns402WithMessageWhenNoActiveSubscriptionIsPresent(Action<ChargebeeSubscription> disableSubscriptionAction)
        {
            // Arrange
            disableSubscriptionAction(_subscription);
            var filter = CreateAutorizeFilter();

            // Act
            await filter.OnAuthorizationAsync(_context);

            // Assert
            Assert.IsType<ObjectResult>(_context.Result);
            var objectResult = (ObjectResult)_context.Result;
            Assert.Equal(StatusCodes.Status402PaymentRequired, objectResult.StatusCode);

            var result = (ProblemDetails)objectResult.Value;
            var errors = (Dictionary<string, bool>)result.Extensions["errors"];
            Assert.False(errors["subscriptionEnabled"]);
        }

        [Fact]
        public async Task Returns402WithMessageWhenNoSubscriptionPresent()
        {
            // Arrange
            _searchFirmId = Guid.NewGuid();
            var filter = CreateAutorizeFilter();

            // Act
            await filter.OnAuthorizationAsync(_context);

            // Assert
            Assert.IsType<ObjectResult>(_context.Result);
            var objectResult = (ObjectResult)_context.Result;
            Assert.Equal(StatusCodes.Status402PaymentRequired, objectResult.StatusCode);

            var result = (ProblemDetails)objectResult.Value;
            var errors = (Dictionary<string, bool>)result.Extensions["errors"];
            Assert.False(errors["subscriptionEnabled"]);
        }

        private ActiveSubscriptionPresentAuthorizeFilter CreateAutorizeFilter()
        {
            var actionContextMock = Mock.Of<ActionContext>(c => c.HttpContext == new DefaultHttpContext()
                                                                && c.RouteData == new RouteData()
                                                                && c.ActionDescriptor == new ActionDescriptor { EndpointMetadata = _endpointMetadata }
                                                                //&& c.ModelState == new ModelStateDictionary()
                                                                );

            _context = new AuthorizationFilterContext(actionContextMock, _filters);

            var builder = new ClassBuilder<ActiveSubscriptionPresentAuthorizeFilter>()
                         .SetSearchFirmUser(_searchFirmId, _userId)
                         .SetFakeRepository(_fakeRepository);

            builder.ServiceCollection.AddTransient<SubscriptionRepository>();

            return builder.Build();
        }
    }
}
