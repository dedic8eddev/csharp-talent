using Ikiru.Parsnips.Api.Controllers.Persons.Documents.Download;
using Ikiru.Parsnips.UnitTests.Helpers;
using Ikiru.Parsnips.Domain;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Blobs;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Persons.Documents.Download
{
    public class GetTests
    {
        private readonly Guid m_SearchFirmId = Guid.NewGuid();
        
        private readonly Person m_StoredPerson;
        private readonly Person m_StoredPersonOther;
        
        private readonly Get.Query m_Query;
        
        private readonly FakeCosmos m_FakeCosmos = new FakeCosmos();
        
        private string PersonDocumentPath => $"{m_SearchFirmId}/{m_StoredPerson.Id}/";

        public GetTests()
        {
            m_StoredPerson = new Person(m_SearchFirmId)
                             {
                                 Name = "Person with Documents",
                                 
                                 Documents = new List<PersonDocument>
                                             {
                                                 new PersonDocument(m_SearchFirmId, "document1.pdf"),
                                                 new PersonDocument(m_SearchFirmId, "document2.docx"),
                                                 new PersonDocument(m_SearchFirmId, "document3.txt"),
                                             }
                             };
            m_StoredPersonOther = new Person(m_SearchFirmId)
                                  {
                                      Name = "Other Person"
                                  };

            m_Query = new Get.Query
                      {
                          DocumentId = m_StoredPerson.Documents[0].Id, // Defaults to First Document
                          PersonId = m_StoredPerson.Id
                      };

            m_FakeCosmos.EnableContainerLinqQuery(FakeCosmos.PersonsContainerName, m_SearchFirmId.ToString(), () => new[] { m_StoredPerson, m_StoredPersonOther });
        }
        
        [Theory]
        [InlineData(0, "application/pdf")]
        [InlineData(1, "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
        [InlineData(2, "text/plain")]
        public async Task GetReturnsCorrectResult(int storedDocumentIndex, string expectedContentType)
        {
            // Given
            var targetStoredDocument = m_StoredPerson.Documents[storedDocumentIndex];
            m_Query.DocumentId = targetStoredDocument.Id;
            var controller = CreateController();

            // When
            var actionResult = await controller.Get(m_Query);

            // Then
            var result = (Get.Result)((OkObjectResult)actionResult).Value;
            Assert.StartsWith($"http://127.0.0.1:10000/devstoreaccount1/{BlobStorage.ContainerNames.PersonsDocuments}/{PersonDocumentPath}{targetStoredDocument.Id}?", result.TemporaryUrl);
            result.TemporaryUrl.AssertThatSaSUrl()
                  .HasStartNoOlderThanSeconds(65)
                  .HasEndNoMoreThanMinutesInFuture(10)
                  .HasSignature()
                  .HasPermissionEquals("r")
                  .HasContentDispositionEquals($"filename={targetStoredDocument.FileName}")
                  .HasContentTypeEquals(expectedContentType);
        }

        [Fact]
        public async Task GetThrowsExceptionIfPersonDoesNotExist()
        {
            // Given
            var missingPersonId = Guid.NewGuid();
            m_Query.PersonId = missingPersonId;
            var controller = CreateController();

            // When
            var ex = await Record.ExceptionAsync(() => controller.Get(m_Query));

            // Then
            ex.AssertNotFoundFailure($"Unable to find 'Person' with Id '{missingPersonId}'");
        }

        [Fact]
        public async Task GetThrowsExceptionIfDocumentDoesNotExist()
        {
            // Given
            var missingDocumentId = Guid.NewGuid();
            m_Query.DocumentId = missingDocumentId;
            var controller = CreateController();

            // When
            var ex = await Record.ExceptionAsync(() => controller.Get(m_Query));

            // Then
            ex.AssertNotFoundFailure($"Unable to find 'Document' with Id '{missingDocumentId}'");
        }

        [Fact]
        public async Task GetThrowsExceptionIfDocumentNotForPerson()
        {
            // Given
            m_Query.PersonId = m_StoredPersonOther.Id;
            var controller = CreateController();

            // When
            var ex = await Record.ExceptionAsync(() => controller.Get(m_Query));

            // Then
            ex.AssertNotFoundFailure($"Unable to find 'Document' with Id '{m_StoredPerson.Documents[0].Id}'");
        }
        
        private DownloadController CreateController()
        {
            return new ControllerBuilder<DownloadController>()
                  .SetFakeCosmos(m_FakeCosmos)
                  .SetSearchFirmUser(m_SearchFirmId)
                  .SetFakeRepository(new FakeRepository())
                  .Build();
        }
    }
}
