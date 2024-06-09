using AutoMapper;
using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Chargebee;
using Ikiru.Parsnips.Functions.SearchFirmSubscriptionEventHandlers.Plan;
using Ikiru.Persistence.Repository;
using MediatR;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Functions.SearchFirmSubscriptionEventHandlers.Plans
{
    public class PlanUpdatedTests
    {
        private Updated.Payload _payload;
        private Mock<IRepository> _respositoryMock;
        private IMapper _mapper;
        private ChargebeePlan _testplan1;
        private ChargebeePlan _testplan2;
        private ChargebeePlan _testplan1changed;

        public PlanUpdatedTests()
        {
            _mapper = new MapperConfiguration(c => 
            c.AddProfile<Ikiru.Parsnips.Functions.SearchFirmSubscriptionEventHandlers.Subscription.MappingProfile>()).CreateMapper();
        }

        [Fact]
        public void PlanUpdatedCreatesPlan()
        {
            var mediator = new Mock<IMediator>();


            Setup();

            var subscriptionRepository = new SubscriptionRepository(_respositoryMock.Object);


            var handler = new Updated.Handler(subscriptionRepository, _mapper );

            //When
            var result = handler.Handle(_payload, new CancellationToken()).Result;

            _respositoryMock.Verify(x => x.Delete<ChargebeePlan>(It.IsAny<string>(),It.IsAny<string>()), Times.Never);
            _respositoryMock.Verify(x => x.GetByQuery<ChargebeePlan>(It.IsAny<Expression<Func<ChargebeePlan, bool>>>()), Times.Once);
            _respositoryMock.Verify(x => x.UpdateItem<ChargebeePlan>(It.Is<ChargebeePlan>(p => 
                                                                                              p.PlanId == "NewPlan" &&
                                                                                              p.PlanType == _payload.Value.Content.Plan.MetaData.PlanType &&
                                                                                              p.DefaultTokens == _payload.Value.Content.Plan.MetaData.DefaultTokens
                                                                                              )), Times.Once);
        }

        public static IEnumerable<object[]> AddonsTestData()
        {
            yield return new object[] { null };
            yield return new object[] { new string[0] };
            yield return new object[] { new[] { "addon1" } };
            yield return new object[] { new[] { "addon1", "addon2", "addon3" } };
        }

        [Theory]
        [MemberData(nameof(AddonsTestData))]
        public async Task PlanUpdatedStoresAddons(string[] addonIds)
        {
            Setup();

            if (addonIds != null)
                _payload.Value.Content.Plan.ApplicableAddons = addonIds.Select(addonId => new ApplicableAddon { Id = addonId}).ToList();

            var subscriptionRepository = new SubscriptionRepository(_respositoryMock.Object);

            var handler = new Updated.Handler(subscriptionRepository, _mapper);

            //When
            await handler.Handle(_payload, new CancellationToken());

            //Then
            _respositoryMock.Verify(x => x.UpdateItem(It.Is<ChargebeePlan>(p => AssertAddons(addonIds, p))));
        }

        private bool AssertAddons(string[] expectedAddonIds, ChargebeePlan plan)
        {
            expectedAddonIds ??= new string[0];

            Assert.Equal(expectedAddonIds, plan.ApplicableAddons);
            return true;
        }

        [Fact]
        public void PlanUpdatedWithStatusActiveUpdatesPlan()
        {
            var mediator = new Mock<IMediator>();

            Setup();
            _respositoryMock.Setup(r => r.GetByQuery(It.IsAny<Expression<Func<ChargebeePlan, bool>>>()))
                .Returns(Task.FromResult(new List<ChargebeePlan>() { _testplan1 }));
            _respositoryMock.Setup(r => r.UpdateItem<ChargebeePlan>(It.Is<ChargebeePlan>(p => p.PlanId == "NewPlan")))
                .Returns(Task.FromResult(_testplan2));

            var subscriptionRepository = new SubscriptionRepository(_respositoryMock.Object);

            var handler = new Updated.Handler(subscriptionRepository, _mapper);

            //When
            var result = handler.Handle(_payload, new CancellationToken()).Result;

            _respositoryMock.Verify(x => x.Delete<ChargebeePlan>(It.IsAny<string>(),It.IsAny<string>()), Times.Never);
            _respositoryMock.Verify(x => x.GetByQuery<ChargebeePlan>(It.IsAny<Expression<Func<ChargebeePlan, bool>>>()), Times.Once);
            _respositoryMock.Verify(x => x.UpdateItem<ChargebeePlan>(It.Is<ChargebeePlan>(p => p.PlanId == "NewPlan")), Times.Once);
        }


        [Fact]
        public void PlanUpdatedWithStatusArchivedDeletesPlan()
        {
            var mediator = new Mock<IMediator>();

            Setup();
            _respositoryMock.Setup(r => r.GetByQuery(It.IsAny<Expression<Func<ChargebeePlan, bool>>>()))
                .Returns(Task.FromResult(new List<ChargebeePlan>() { _testplan1 }));
            _payload.Value.Content.Plan.Status = Domain.Enums.PlanStatus.Archived;

            var subscriptionRepository = new SubscriptionRepository(_respositoryMock.Object);

            var handler = new Updated.Handler(subscriptionRepository, _mapper);

            //When
            var result = handler.Handle(_payload, new CancellationToken()).Result;

            _respositoryMock.Verify(x => x.Delete<ChargebeePlan>(It.IsAny<string>(),It.IsAny<string>()), Times.Once);
            _respositoryMock.Verify(x => x.GetByQuery<ChargebeePlan>(It.IsAny<Expression<Func<ChargebeePlan, bool>>>()), Times.Once);
            _respositoryMock.Verify(x => x.UpdateItem<ChargebeePlan>(It.Is<ChargebeePlan>(p => p.PlanId == "NewPlan")), Times.Never);
        }


        private void Setup()
        {
            _payload = new Updated.Payload()
            {
                Value = new ChargebeeEventPayload()
                {
                    EventType = EventTypeEnum.PlanCreated,
                    Content = new Content()
                    {
                        Plan = new Plan()
                        {
                            Id = "NewPlan",
                            Price = 1200,
                            Status = Domain.Enums.PlanStatus.Active,
                            PeriodUnit = Domain.Enums.PeriodUnitEnum.Year,
                            MetaData = new PlanMetaData()
                            {
                                DefaultTokens = 40,
                                PlanType = Domain.Enums.PlanType.Connect
                            }

                        }
                    }
                }
            };

            _testplan1 = new ChargebeePlan()
            {
                Id = Guid.Parse("74FF5673-779C-4290-A7C3-743FD7F298A7"),
                PlanId = "NewPlan",
                PeriodUnit = Domain.Enums.PeriodUnitEnum.Year,
                Period = 1,
                PlanType = Domain.Enums.PlanType.Basic,
                CurrencyCode = "GBP",
                Price = 1300,
                CanPurchaseRocketReach = true,
                DefaultTokens = 40
            };


            _testplan2 = new ChargebeePlan()
            {
                Id = Guid.Parse("74FF5673-779C-4290-A7C3-743FD7F298A7"),
                PlanId = "Good-Plan",
                PeriodUnit = Domain.Enums.PeriodUnitEnum.Year,
                Period = 1,
                PlanType = Domain.Enums.PlanType.Basic,
                CurrencyCode = "GBP",
                Price = 1300,
                CanPurchaseRocketReach = true,
                DefaultTokens = 40
            };

            _testplan1changed = new ChargebeePlan()
            {
                Id = Guid.Parse("A1C8DAFD-D6A6-422D-8436-5134D96211F6"),
                PlanId = "Good-Plan",
                PeriodUnit = Domain.Enums.PeriodUnitEnum.Year,
                Period = 1,
                PlanType = Domain.Enums.PlanType.Basic,
                CurrencyCode = "GBP",
                CanPurchaseRocketReach = false,
                DefaultTokens = 40
            };

            _respositoryMock = new Mock<IRepository>();

            _respositoryMock.Setup(r => r.Delete<ChargebeePlan>(It.Is<string>(x => x == "74FF5673-779C-4290-A7C3-743FD7F298A7".ToLower()),It.Is<string>(x => x == "74FF5673-779C-4290-A7C3-743FD7F298A7".ToLower())))
                .Returns(Task.FromResult(true));
            _respositoryMock.Setup(r => r.Delete<ChargebeePlan>(It.Is<string>(x => x == "A1C8DAFD-D6A6-422D-8436-5134D96211F6".ToLower()),It.Is<string>(x => x == "A1C8DAFD-D6A6-422D-8436-5134D96211F6".ToLower())))
                .Returns(Task.FromResult(false));
            _respositoryMock.Setup(r => r.UpdateItem<ChargebeePlan>(It.Is<ChargebeePlan>(p => p.Id == _testplan1changed.Id)))
                .Returns(Task.FromResult(_testplan1));
            _respositoryMock.Setup(r => r.UpdateItem<ChargebeePlan>(It.Is<ChargebeePlan>(p => p.Id == _testplan1changed.Id)))
                .Returns(Task.FromResult(_testplan1));
            _respositoryMock.Setup(r => r.UpdateItem<ChargebeePlan>(It.Is<ChargebeePlan>(p => p.PlanId == "NewPlan")))
                .Returns(Task.FromResult(_testplan1));
            _respositoryMock.Setup(r => r.GetByQuery(It.IsAny<Expression<Func<ChargebeePlan, bool>>>()))
                .Returns(Task.FromResult(new List<ChargebeePlan>() {}));
        }

    }
}
