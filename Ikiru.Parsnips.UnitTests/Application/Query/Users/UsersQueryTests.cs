using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Application.Query.Users;
using Ikiru.Parsnips.Domain;
using Ikiru.Persistence.Repository;
using Moq;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;
using Ikiru.Parsnips.Application.Query.Users.Models;

namespace Ikiru.Parsnips.UnitTests.Application.Query.Users
{
    public class UsersQueryTests
    {
        private Mock<IRepository> _repositoryMock;

        [Fact]
        public async Task GetActiveUsersForSearchFirm()
        {
            var searchFirmId = Guid.NewGuid();

            _repositoryMock = new Mock<IRepository>();
            Expression<Func<SearchFirmUser, bool>> expression = s => s.Discriminator == SearchFirmUser.DiscriminatorName
                                                                            && s.SearchFirmId == searchFirmId
                                                                            && s.IsEnabled == true;

            _repositoryMock.Setup(x => x.Count(It.IsAny<Expression<Func<SearchFirmUser, bool>>>()))
                .Returns(Task.FromResult(1));

            var searchFirmRepository = new SearchFirmRepository(_repositoryMock.Object);

            var userQuery = new UserQuery(searchFirmRepository);
            var response = await userQuery.Handle(new GetActiveUsersRequest()
            {
                SearchFirmId = searchFirmId
            });

            Assert.Equal(1, response.Count);
            _repositoryMock.Verify(x => x.Count(It.Is<Expression<Func<SearchFirmUser, bool>>>(
                                             e => Neleus.LambdaCompare.Lambda.Eq(e, expression))),
                                        Times.Once());
        }
    }
}
