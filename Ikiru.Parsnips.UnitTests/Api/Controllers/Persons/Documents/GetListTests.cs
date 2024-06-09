using Ikiru.Parsnips.Api.Controllers.Persons.Documents;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.UnitTests.Helpers;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Persons.Documents
{
    public class GetListTests
    {
        private readonly Guid m_SearchFirmId = Guid.NewGuid();
        
        private readonly Person m_StoredPerson;

        private readonly Guid m_MissingPersonId = Guid.NewGuid();

        private readonly GetList.Query m_Query = new GetList.Query();
        
        private readonly FakeCosmos m_FakeCosmos = new FakeCosmos();

        public GetListTests()
        {
            m_StoredPerson = new Person(m_SearchFirmId)
                             {
                                 Name = "Person with Documents",
                                 
                                 Documents = new List<PersonDocument>
                                             {
                                                 new PersonDocument(m_SearchFirmId, "document1.docx"),
                                                 new PersonDocument(m_SearchFirmId, "document2.pdf")
                                             }
                             };

            m_FakeCosmos.EnableContainerLinqQuery(FakeCosmos.PersonsContainerName, m_SearchFirmId.ToString(), () => new [] { m_StoredPerson });
        }
        
        [Fact]
        public async Task GetListReturnsCorrectResults()
        {
            // Given
            var controller = CreateController();

            // When
            var actionResult = await controller.GetList(m_StoredPerson.Id, m_Query);

            // Then
            var result = (GetList.Result)((OkObjectResult)actionResult).Value;
            Assert.NotEmpty(result.Documents);
            Assert.Equal(2, result.Documents.Count);

            var firstDocument = result.Documents[0];
            Assert.Equal(m_StoredPerson.Documents[0].Id, firstDocument.Id);
            Assert.Equal(m_StoredPerson.Documents[0].FileName, firstDocument.Filename);
            Assert.Equal(m_StoredPerson.Documents[0].CreatedDate, firstDocument.CreatedDate);

            var secondDocument = result.Documents[1];
            Assert.Equal(m_StoredPerson.Documents[1].Id, secondDocument.Id);
            Assert.Equal(m_StoredPerson.Documents[1].FileName, secondDocument.Filename);
            Assert.Equal(m_StoredPerson.Documents[1].CreatedDate, secondDocument.CreatedDate);
        }

        [Fact]
        public async Task GetListThrowsExceptionIfPersonDoesNotExist()
        {
            // Given
            var controller = CreateController();

            // When
            var ex = await Record.ExceptionAsync(() => controller.GetList(m_MissingPersonId, m_Query));

            // Then
            ex.AssertNotFoundFailure($"Unable to find 'Person' with Id '{m_MissingPersonId}'");
        }
        
        private DocumentsController CreateController()
        {
            return new ControllerBuilder<DocumentsController>()
                  .SetFakeCosmos(m_FakeCosmos)
                  .SetSearchFirmUser(m_SearchFirmId)
                  .SetFakeRepository(new FakeRepository())
                  .Build();
        }
    }
}
