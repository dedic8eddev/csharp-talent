using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Application.Query.Users;
using Ikiru.Parsnips.Application.Query.Users.Models;
using Ikiru.Parsnips.Application.Shared.Models;
using Ikiru.Parsnips.Domain;
using Ikiru.Persistence.Repository;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Application.Query.Users
{
    public class GetUserDetailsByUserIdQueryTests
    {
        private const string _planId = "planId123";

        private readonly GetUserDetailsByUserIdQuery _getUserDetailsByUserIdQuery;
        private readonly Mock<IRepository> _repositoryMock = new Mock<IRepository>();
        private readonly Guid _searchFirmId = Guid.NewGuid();
        private readonly Guid _userId = Guid.NewGuid();
        private DateTimeOffset _subscriptionCurrentEndTerm;

        public GetUserDetailsByUserIdQueryTests()
        {
            _getUserDetailsByUserIdQuery = new GetUserDetailsByUserIdQuery(new SearchFirmRepository(_repositoryMock.Object),
                                            new SubscriptionRepository(_repositoryMock.Object));

            Setup();
        }

        private void Setup()
        {
            _repositoryMock.Setup(r => r.GetByQuery(It.IsAny<Expression<Func<ChargebeePlan, bool>>>()))
                .Returns(Task.FromResult(new List<ChargebeePlan>()
                {
                    new ChargebeePlan()
                    {
                        PlanId = _planId,
                        PlanType = Domain.Enums.PlanType.Connect,
                        Status = Domain.Enums.PlanStatus.Active
                    }
                }));

            _repositoryMock.Setup(r => r.GetItem<SearchFirmUser>(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(new SearchFirmUser(_searchFirmId)
                {
                    UserRole = Domain.Enums.UserRole.TeamMember
                }));
        }

        [Fact]
        public async Task GetUserDetailsByUserIdReturnsDisabledSubscriptionWhenNoSubscriptionPresent()
        {
            // Arrange
            _repositoryMock
               .Setup(r => r.GetByQuery(It.IsAny<string>(),
                                        It.IsAny<Expression<Func<IOrderedQueryable<ChargebeeSubscription>, IQueryable<ChargebeeSubscription>>>>(),
                                        It.IsAny<int?>()))
               .ReturnsAsync(new List<ChargebeeSubscription>());


            var query = new GetUserDetailsByUserIdRequest
            {
                SearchFirmId = _searchFirmId,
                UserId = _userId
            };

            // Act
            var result = await _getUserDetailsByUserIdQuery.Handle(query);

            // Assert
            Assert.True(result.IsSubscriptionExpired);
            Assert.Equal(UserRole.TeamMember, result.UserRole);
        }

        [Fact]
        public async Task GetUserDetailsByUserIdQueryNotExpiredUser()
        {
            // Arrange
            _repositoryMock.Setup(r => r.GetItem<SearchFirm>(It.IsAny<string>(),
                                                            It.Is<string>(s => s == _searchFirmId.ToString())))
                .Returns(Task.FromResult(new SearchFirm()));

            _subscriptionCurrentEndTerm = DateTime.Now.AddHours(1);

            _repositoryMock
               .Setup(r => r.GetByQuery(It.IsAny<string>(),
                                        It.IsAny<Expression<Func<IOrderedQueryable<ChargebeeSubscription>, IQueryable<ChargebeeSubscription>>>>(),
                                        It.IsAny<int?>()))
               .ReturnsAsync(new List<ChargebeeSubscription>()
                             {
                                 new ChargebeeSubscription(_searchFirmId)
                                 {
                                     PlanId = _planId,
                                     CurrentTermEnd = _subscriptionCurrentEndTerm,
                                     Status = Domain.Chargebee.Subscription.StatusEnum.Active
                                 }
                             });


            var query = new GetUserDetailsByUserIdRequest
            {
                SearchFirmId = _searchFirmId,
                UserId = _userId
            };

            // Act
            var result = await _getUserDetailsByUserIdQuery.Handle(query);

            // Assert
            Assert.False(result.IsSubscriptionExpired);
            Assert.Equal(UserRole.TeamMember, result.UserRole);
            Assert.Equal(_subscriptionCurrentEndTerm, result.SubscriptionExpired);
            Assert.Equal(Domain.Enums.PlanType.Connect.ToString(), result.PlanType);

        }
    }
}
