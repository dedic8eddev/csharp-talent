using Ikiru.Parsnips.Api.Controllers.Persons.Documents;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ResourceNotFound;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Blobs;
using Ikiru.Parsnips.UnitTests.Helpers;
using Ikiru.Parsnips.UnitTests.Helpers.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Persons.Documents
{
    public class PostTests
    {
        private const string _FILENAME = "CV.docx";

        private readonly FakeCosmos m_FakeCosmos;
        private readonly Post.Command m_Command;
        private readonly Person m_Person;

        private readonly FakeCloud m_FakeCloud = new FakeCloud();
        private readonly Stream m_Stream = new MemoryStream(Encoding.UTF8.GetBytes("This is a content of a CV."));
        private readonly Guid m_SearchFirmId = Guid.NewGuid();

        private readonly List<Guid> m_DocumentIds = new List<Guid>();
        private readonly Guid m_MissingPersonId = Guid.NewGuid();

        public PostTests()
        {
            m_Person = new Person(m_SearchFirmId, linkedInProfileUrl: "https://www.linkedin.com/in/john-smith") { TaggedEmails = new List<TaggedEmail> { new TaggedEmail{ Email = "tagged@emai.co.uk", SmtpValid = "valid" } }};

            m_FakeCosmos = new FakeCosmos()
                 .EnableContainerFetch(FakeCosmos.PersonsContainerName, m_Person.Id.ToString(), m_SearchFirmId.ToString(), () => m_Person)
                 .EnableContainerFetchThrowCosmosException<Person>(FakeCosmos.PersonsContainerName, m_MissingPersonId.ToString(), m_SearchFirmId.ToString(), HttpStatusCode.NotFound);

            m_FakeCosmos.PersonsContainer
                .Setup(c => c.ReplaceItemAsync(It.IsAny<Person>(), It.Is<string>(i => i == m_Person.Id.ToString()), It.Is<PartitionKey>(p => p.Equals(new PartitionKey(m_SearchFirmId.ToString()))), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                     .Callback((Person c, string id, PartitionKey? pk, ItemRequestOptions o, CancellationToken ct) => m_DocumentIds.AddRange(c.Documents.Select(d => d.Id)))
                     .ReturnsAsync(Mock.Of<ItemResponse<Person>>());

            var mockFormFile = new Mock<IFormFile>();
            mockFormFile.Setup(f => f.FileName)
                        .Returns(_FILENAME);
            mockFormFile.Setup(f => f.OpenReadStream())
                        .Returns(m_Stream);

            m_Command = new Post.Command { File = mockFormFile.Object };
        }

        [Fact]
        public async Task PostReturnsOk()
        {
            // Given
            var controller = CreateController();

            // When
            var actionResult = await controller.Post(m_Person.Id, m_Command);

            // Then
            Assert.True(actionResult is OkObjectResult);
            var result = (Post.Result)((OkObjectResult)actionResult).Value;
            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Equal(_FILENAME, result.Filename);
            Assert.Equal(DateTimeOffset.Now.Date, result.CreatedDate.Date);
        }

        [Fact]
        public async Task PostCreatesItemInContainer()
        {
            // Given
            var controller = CreateController();

            // When
            await controller.Post(m_Person.Id, m_Command);

            // Then
            var container = m_FakeCosmos.PersonsContainer;
            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(p => p.Documents.Single().Id == m_DocumentIds.Single()), It.Is<string>(i => i == m_Person.Id.ToString()), It.Is<PartitionKey>(p => p.Equals(new PartitionKey(m_SearchFirmId.ToString()))), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(p => p.Documents.Single().FileName == _FILENAME), It.Is<string>(i => i == m_Person.Id.ToString()), It.Is<PartitionKey>(p => p.Equals(new PartitionKey(m_SearchFirmId.ToString()))), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(p => p.Documents.Single().CreatedDate.Date == DateTime.UtcNow.Date), It.Is<string>(i => i == m_Person.Id.ToString()), It.Is<PartitionKey>(p => p.Equals(new PartitionKey(m_SearchFirmId.ToString()))), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));

            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(p => p.Id == m_Person.Id), It.Is<string>(i => i == m_Person.Id.ToString()), It.Is<PartitionKey>(p => p.Equals(new PartitionKey(m_SearchFirmId.ToString()))), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(p => p.CreatedDate == m_Person.CreatedDate), It.Is<string>(i => i == m_Person.Id.ToString()), It.Is<PartitionKey>(p => p.Equals(new PartitionKey(m_SearchFirmId.ToString()))), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(p => p.Name == m_Person.Name), It.Is<string>(i => i == m_Person.Id.ToString()), It.Is<PartitionKey>(p => p.Equals(new PartitionKey(m_SearchFirmId.ToString()))), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(p => p.Location == m_Person.Location), It.Is<string>(i => i == m_Person.Id.ToString()), It.Is<PartitionKey>(p => p.Equals(new PartitionKey(m_SearchFirmId.ToString()))), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(p => p.TaggedEmails.AssertSameList(m_Person.TaggedEmails)), It.Is<string>(i => i == m_Person.Id.ToString()), It.Is<PartitionKey>(p => p.Equals(new PartitionKey(m_SearchFirmId.ToString()))), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(p => p.PhoneNumbers.IsSameList(m_Person.PhoneNumbers)), 
                It.Is<string>(i => i == m_Person.Id.ToString()), 
                It.Is<PartitionKey>(p => p.Equals(new PartitionKey(m_SearchFirmId.ToString()))), 
                It.IsAny<ItemRequestOptions>(), 
                It.IsAny<CancellationToken>()));
            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(p => p.JobTitle == m_Person.JobTitle), It.Is<string>(i => i == m_Person.Id.ToString()), It.Is<PartitionKey>(p => p.Equals(new PartitionKey(m_SearchFirmId.ToString()))), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(p => p.Organisation == m_Person.Organisation), It.Is<string>(i => i == m_Person.Id.ToString()), It.Is<PartitionKey>(p => p.Equals(new PartitionKey(m_SearchFirmId.ToString()))), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(p => p.LinkedInProfileUrl == m_Person.LinkedInProfileUrl), It.Is<string>(i => i == m_Person.Id.ToString()), It.Is<PartitionKey>(p => p.Equals(new PartitionKey(m_SearchFirmId.ToString()))), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(p => p.LinkedInProfileId == m_Person.LinkedInProfileId), It.Is<string>(i => i == m_Person.Id.ToString()), It.Is<PartitionKey>(p => p.Equals(new PartitionKey(m_SearchFirmId.ToString()))), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(p => p.SearchFirmId == m_Person.SearchFirmId), It.Is<string>(i => i == m_Person.Id.ToString()), It.Is<PartitionKey>(p => p.Equals(new PartitionKey(m_SearchFirmId.ToString()))), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task PostUploadStreamToBlobStorage()
        {
            // Given
            var controller = CreateController();

            // When
            await controller.Post(m_Person.Id, m_Command);

            // Then

            var expectedBlobPath = $"{BlobStorage.ContainerNames.PersonsDocuments}/{m_SearchFirmId}/{m_Person.Id}/{m_DocumentIds.Single()}";

            var (blobPath, blobClientMock) = m_FakeCloud.BlobClients.Single();
            Assert.Equal(expectedBlobPath, blobPath);

            blobClientMock.Verify(b => b.UploadAsync(It.Is<Stream>(s => s == m_Stream),
                                                     null, null, null, null, null, default, default));
        }

        [Fact]
        public async Task PostAllowsDuplicateFileName()
        {
            // Given
            var controller = CreateController();

            // When
            await controller.Post(m_Person.Id, m_Command);
            await controller.Post(m_Person.Id, m_Command);

            // Then
            var container = m_FakeCosmos.PersonsContainer;
            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(p => p.Documents.Any(d => d.Id == m_DocumentIds[0])), It.Is<string>(i => i == m_Person.Id.ToString()), It.Is<PartitionKey>(p => p.Equals(new PartitionKey(m_SearchFirmId.ToString()))), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task PostThrowsResourceNotFoundIfNoPerson()
        {
            // Given
            var controller = CreateController();

            // When
            var ex = await Record.ExceptionAsync(() => controller.Post(m_MissingPersonId, m_Command));

            // Then
            Assert.NotNull(ex);
            Assert.IsType<ResourceNotFoundException>(ex);
        }

        [Fact]
        public async Task PostThrowsSameExceptionCosmosThrowsWhenNot404()
        {
            // Given
            var personId = Guid.NewGuid();
            m_FakeCosmos.EnableContainerFetchThrowCosmosException<Person>(FakeCosmos.PersonsContainerName, personId.ToString(), m_SearchFirmId.ToString(), HttpStatusCode.Unauthorized, out var expectedException);
            var controller = CreateController();

            // When
            var ex = await Record.ExceptionAsync(() => controller.Post(personId, m_Command));

            // Then
            Assert.NotNull(ex);
            Assert.Same(expectedException, ex);
        }

        [Fact]
        public async Task PostDoesNotCreateItemInContainerWhenBlobStorageThrows()
        {
            // Given
            m_FakeCloud.BlobServiceClient.Setup(c => c.GetBlobContainerClient(It.IsAny<string>()))
                       .Throws(new Exception("A random exception while storing blob into storage."));
            var controller = CreateController();

            // When
            var ex = await Record.ExceptionAsync(() => controller.Post(m_Person.Id, m_Command));

            // Then
            Assert.NotNull(ex);
            m_FakeCosmos.PersonsContainer.Verify(c => c.ReplaceItemAsync(It.IsAny<Person>(), It.IsAny<string>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()), Times.Never());
        }

        private DocumentsController CreateController()
          => new ControllerBuilder<DocumentsController>()
            .SetFakeCloud(m_FakeCloud)
            .SetFakeCosmos(m_FakeCosmos)
            .SetSearchFirmUser(m_SearchFirmId)
            .SetFakeRepository(new FakeRepository())
            .Build();
    }
}
