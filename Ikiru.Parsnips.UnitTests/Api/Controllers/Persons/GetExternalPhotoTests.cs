using Ikiru.Parsnips.Api.Controllers.Persons;
using Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Threading.Tasks;
using Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi.Models.Person;
using Xunit;
using System.Threading;
using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Application.Infrastructure.DataPool;
using Ikiru.Persistence.Repository;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Persons
{
    public class GetExternalPhotoTests
    {
        private static string m_Url = "https://personId/photo/url";

        private readonly Mock<IDataPoolApi> m_MockDataPoolApi;
        private readonly GetExternalPhoto.Query m_Query = new GetExternalPhoto.Query { PersonId = Guid.NewGuid() };
        private readonly Mock<IRepository> m_RepositoryMock = new Mock<IRepository>();
        private readonly Mock<IPersonInfrastructure> m_PersonInfrastructure = new Mock<IPersonInfrastructure>();

        public GetExternalPhotoTests()
        {
            m_MockDataPoolApi = new Mock<IDataPoolApi>();
            m_MockDataPoolApi
               .Setup(api => api.GetPersonPhotoUrl(It.Is<Guid>(id => id == m_Query.PersonId), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new PersonPhoto { Photo = new PersonPhoto.PersonPhotoUrl { Url = m_Url } });
        }

        [Fact]
        public async Task GetExternalPhotoReturnsUrl()
        {
            // Given
            var controller = CreateController();

            // When
            var actionResult = await controller.GetExternalPhoto(m_Query);

            // Then
            var result = (GetExternalPhoto.Result)((OkObjectResult)actionResult).Value;
            Assert.Equal(m_Url, result.Photo.Url);
        }

        [Fact]
        public async Task GetExternalPhotoReturnsNullWhenNoData()
        {
            // Given
            var controller = CreateController();

            // When
            var actionResult = await controller.GetExternalPhoto(new GetExternalPhoto.Query { PersonId = Guid.NewGuid() });

            // Then
            var result = (GetExternalPhoto.Result)((OkObjectResult)actionResult).Value;
            Assert.Null(result.Photo);
        }

        private PersonsController CreateController()
            => new ControllerBuilder<PersonsController>()
                  .AddTransient(m_MockDataPoolApi.Object)
                  .AddTransient(m_RepositoryMock.Object)
                .AddTransient(m_PersonInfrastructure.Object)
                  .Build();
    }
}
