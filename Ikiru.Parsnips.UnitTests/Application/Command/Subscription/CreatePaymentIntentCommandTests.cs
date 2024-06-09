using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ikiru.Parsnips.Application.Command.Subscription;
using Ikiru.Parsnips.Application.Command.Subscription.Models;
using Ikiru.Parsnips.Application.Infrastructure.Subscription;
using Ikiru.Parsnips.Application.Infrastructure.Subscription.Models;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure;
using Ikiru.Parsnips.UnitTests.Helpers;
using Moq;
using Xunit;


namespace Ikiru.Parsnips.UnitTests.Application.Command.Subscription
{
    public class CreatePaymentIntentCommandTests
    {
        private readonly string _customerId = "customer-id";
        private readonly string _connectPlanId = "connect-plan-id";
        private readonly string _addonId = "addon-id";

        private readonly int _defaultToken = 19;

        private readonly FakeRepository _fakeRepository = new FakeRepository();
        private readonly Mock<ISubscription> _subscriptionMock = new Mock<ISubscription>();

        private readonly CreatePaymentIntentRequest _request;

        public CreatePaymentIntentCommandTests()
        {
            var connectPlan = new ChargebeePlan
                           {
                               PlanId = _connectPlanId,
                               CurrencyCode = "GBP",
                               DefaultTokens = _defaultToken,
                               PlanType = PlanType.Connect,
                               PeriodUnit = PeriodUnitEnum.Month,
                               Period = 1,
                               ApplicableAddons = new List<string> { _addonId }
                           };

            var searchFirm = new SearchFirm { ChargebeeCustomerId = _customerId };

            _request = new CreatePaymentIntentRequest { SearchFirmId = searchFirm.SearchFirmId, UnitQuantity = 2, SubscriptionPlanId = _connectPlanId, Couponids = new List<string> { "CouponId1", "CouponId2" } };

            var user1 = new SearchFirmUser(searchFirm.Id);
            var user2 = new SearchFirmUser(searchFirm.Id);
            var disabledUser = new SearchFirmUser(searchFirm.Id) { IsEnabled = false };
            _fakeRepository.AddToRepository(searchFirm, connectPlan, user1, user2, disabledUser);

            _subscriptionMock.Setup(s => s.GetEstimateForSubscription(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(),
                                                                      It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<string>(),
                                                                      It.IsAny<string>()))
                             .ReturnsAsync(new SubscriptionEstimate());
        }

        [Fact]
        public async Task HandleThrowsIfBuyingLessSubscriptionsThanUsers()
        {
            // Arrange
            _request.UnitQuantity = 1;
            var commandHandler = CreateCommandHandler();

            // Act
            var ex = await Record.ExceptionAsync(() => commandHandler.Handle(_request));

            // Assert
            Assert.NotNull(ex);
            Assert.IsType<ParamValidationFailureException>(ex);
        }

        [Fact]
        public async Task HandleDoesNotThrowIfBuyingCorrectNumberOfSubscriptions()
        {
            // Arrange
            var commandHandler = CreateCommandHandler();

            // Act
            var ex = await Record.ExceptionAsync(() => commandHandler.Handle(_request));

            // Assert
            Assert.Null(ex);
        }

        private CreatePaymentIntentCommand CreateCommandHandler()
        {
            return new ServiceBuilder<CreatePaymentIntentCommand>()
                  .SetFakeRepository(_fakeRepository)
                  .AddTransient(_subscriptionMock.Object)
                  .Build();
        }
    }
}
