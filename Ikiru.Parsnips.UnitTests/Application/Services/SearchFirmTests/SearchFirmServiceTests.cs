using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Application.Services;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi;
using Ikiru.Parsnips.UnitTests.Helpers;
using Ikiru.Persistence.Repository;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Application.Services.SearchFirmTests
{
    public class SearchFirmServiceTests
    {
        private readonly Mock<IRepository> _repositoryMock;
        private readonly Mock<IIdentityAdminApi> _identityAdminApi;
        public SearchFirmServiceTests()
        {
            _repositoryMock = new Mock<IRepository>();
            _identityAdminApi = new Mock<IIdentityAdminApi>();
        }

        [Fact]
        public void SetupSearchFirmInWithInitialLoginSet()
        {
            // Act
            var searchFirm = new Domain.SearchFirm();

            // Assert
            Assert.False(searchFirm.PassedInitialLogin);
        }

        [Fact]
        public async Task SetSearchFirmPassedInitialLogin()
        {
            // Arrange
            var searchFirm = new SearchFirm();
            
            _repositoryMock.Setup(r => r.GetItem<SearchFirm>(It.IsAny<string>(), It.Is<string>(s => s == searchFirm.Id.ToString())))
                .Returns(Task.FromResult(new SearchFirm()));

            // Act
            var searchFirmService = new SearchFirmService(new SubscriptionRepository(_repositoryMock.Object),
                                                          new SearchFirmRepository(_repositoryMock.Object),
                                                                                    _identityAdminApi.Object);

            await searchFirmService.PassedInitialLogin(searchFirm.Id);

            // Assert
            _repositoryMock.Verify(s => s.UpdateItem(It.Is<SearchFirm>(s => s.PassedInitialLogin == true)), Times.Once);
        }
    }
}
