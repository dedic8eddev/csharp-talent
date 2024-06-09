using Azure;
using Ikiru.Parsnips.Api.Controllers.Persons.Photo;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Blobs;
using Ikiru.Parsnips.UnitTests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Persons.Photo
{
    public class GetTests
    {
        private readonly Guid m_SearchFirmId = Guid.NewGuid();

        private readonly Get.Query m_Query;
        private readonly Get.Query m_NoPhotoQuery;

        private readonly FakeCloud m_FakeCloud = new FakeCloud();
        private readonly string m_PhotoBlobPath;

        public GetTests()
        {
            m_Query = new Get.Query { PersonId = Guid.NewGuid() };
            m_NoPhotoQuery = new Get.Query { PersonId = Guid.NewGuid() };

            m_PhotoBlobPath = $"{m_SearchFirmId}/{m_Query.PersonId}/photo";
            var noPhotoBlobPath = $"{m_SearchFirmId}/{m_NoPhotoQuery.PersonId}/photo";

            // ReSharper disable once RedundantBoolCompare - for better readability
            m_FakeCloud.SeedFor(BlobStorage.ContainerNames.PersonsDocuments, m_PhotoBlobPath)
                       .Setup(c => c.ExistsAsync(It.IsAny<CancellationToken>()))
                       .Returns<CancellationToken>(_ => Task.FromResult(Mock.Of<Response<bool>>(r => r.Value == true)));

            m_FakeCloud.SeedFor(BlobStorage.ContainerNames.PersonsDocuments, noPhotoBlobPath)
                       .Setup(c => c.ExistsAsync(It.IsAny<CancellationToken>()))
                       .Returns<CancellationToken>(_ => Task.FromResult(Mock.Of<Response<bool>>(r => r.Value == false)));
        }

        [Fact]
        public async Task GetReturnsAddressWhenPhotoIsPresent()
        {
            // Given
            var controller = CreateController();

            // When
            var actionResult = await controller.Get(m_Query);

            // Then
            var result = (Get.Result)((OkObjectResult)actionResult).Value;

            Assert.StartsWith($"{FakeCloud.BASE_URL}/{BlobStorage.ContainerNames.PersonsDocuments}/{m_PhotoBlobPath}?", result.Photo.Url);

            result.Photo.Url.AssertThatSaSUrl()
                  .HasStartNoOlderThanSeconds(65)
                  .HasEndNoMoreThanMinutesInFuture(10)
                  .HasSignature()
                  .HasPermissionEquals("r");
        }

        [Fact]
        public async Task GetReturnsNullWhenNoPhoto()
        {
            // Given
            var controller = CreateController();

            // When
            var actionResult = await controller.Get(m_NoPhotoQuery);

            // Then
            var result = (Get.Result)((OkObjectResult)actionResult).Value;
            Assert.Null(result.Photo);
        }

        private PhotoController CreateController()
        {
            return new ControllerBuilder<PhotoController>()
                  .SetFakeCloud(m_FakeCloud)
                  .SetSearchFirmUser(m_SearchFirmId)
                  .Build();
        }
    }
}
