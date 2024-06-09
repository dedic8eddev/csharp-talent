using Ikiru.Parsnips.Api.Controllers.Persons.Keywords;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ResourceNotFound;
using Ikiru.Parsnips.UnitTests.Helpers;
using Ikiru.Parsnips.UnitTests.Helpers.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Moq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Persons.Keywords
{
    public class DeleteTests
    {
        private const string _KEYWORD1 = "Personal skills";
        private const string _KEYWORDDUPLICATE = "Personal skills";
        private const string _KEYWORD2 = "Another skills";
        private const string _KEYWORD3 = "skills";
        private const string _KEYWORD4 = "personal skills";

        private Delete.Command m_Command;
        private readonly Person m_Person;
        private readonly Guid m_MissingPersonId = Guid.NewGuid();
        private readonly Guid m_SearchFirmId = Guid.NewGuid();
        private readonly FakeCosmos m_FakeCosmos;

        public DeleteTests()
        {
            m_Person = new Person(m_SearchFirmId, linkedInProfileUrl: "https://www.linkedin.com/in/john-smith")
            {
                Documents = new List<PersonDocument> { new PersonDocument(m_SearchFirmId, "CV.docx") },
                Name = "John Smith",
                Location = "Basingstoke",
                TaggedEmails = new List<TaggedEmail> { new TaggedEmail { Email = "john@smith.com" } },
                PhoneNumbers = new List<string> { "0123456" },
                JobTitle = "Manager",
                Organisation = "Smith & Co",
                Keywords = new List<string> { _KEYWORD1, _KEYWORD2, _KEYWORD3, _KEYWORD4, _KEYWORDDUPLICATE },
                GdprLawfulBasisState = new PersonGdprLawfulBasisState
                {
                    GdprLawfulBasisOption = GdprLawfulBasisOptionEnum.LegitimateInterest,
                    GdprLawfulBasisOptionsStatus = GdprLawfulBasisOptionsStatusEnum.NotificationSent,
                    GdprDataOrigin = "Data provided when applied for a position"
                }
            };

            m_FakeCosmos = new FakeCosmos()
                 .EnableContainerFetch(FakeCosmos.PersonsContainerName, m_Person.Id.ToString(), m_SearchFirmId.ToString(), () => m_Person)
                 .EnableContainerFetchThrowCosmosException<Person>(FakeCosmos.PersonsContainerName, m_MissingPersonId.ToString(), m_SearchFirmId.ToString(), HttpStatusCode.NotFound)
                 .EnableContainerReplace<Person>(FakeCosmos.PersonsContainerName, m_Person.Id.ToString(), m_SearchFirmId.ToString());

        }

        [Fact]
        public async Task DeleteKeywordReturnOk()
        {
            // Given
            var controller = CreateController();
            m_Command = new Delete.Command { Keyword = _KEYWORD1 };
            var container = m_FakeCosmos.PersonsContainer;

            // When
            var actionResult = await controller.Delete(m_Person.Id, m_Command);

            // Then
            Assert.True(actionResult is OkResult);


            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(p => (!p.Keywords.Contains(_KEYWORD1))),
                                                    It.Is<string>(i => i == m_Person.Id.ToString()),
                                                    It.Is<PartitionKey>(p => p.Equals(new PartitionKey(m_SearchFirmId.ToString()))),
                                                    It.IsAny<ItemRequestOptions>(),
                                                    It.IsAny<CancellationToken>()));

        }

        [Fact]
        public async Task DeleteKeywordOnlyRemovesMatchingKeywords()
        {
            // Given
            var controller = CreateController();
            m_Command = new Delete.Command { Keyword = _KEYWORD1 };
            var container = m_FakeCosmos.PersonsContainer;

            // When
            var actionResult = await controller.Delete(m_Person.Id, m_Command);

            // Then
            Assert.True(actionResult is OkResult);

            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(p => !p.Keywords.Exists(a => a == _KEYWORD1) &&
                                                                        p.Keywords.Exists(a => a == _KEYWORD2) &&
                                                                        p.Keywords.Exists(a => a == _KEYWORD3) &&
                                                                        p.Keywords.Exists(a => a == _KEYWORD4) &&
                                                                        p.Organisation == m_Person.Organisation &&
                                                                        p.Location == m_Person.Location &&
                                                                        p.PhoneNumbers.IsSameList(m_Person.PhoneNumbers) &&
                                                                        p.LinkedInProfileUrl == m_Person.LinkedInProfileUrl &&
                                                                        p.TaggedEmails.AssertSameList(m_Person.TaggedEmails) &&
                                                                        p.ImportedLinkedInProfileUrl == m_Person.ImportedLinkedInProfileUrl &&
                                                                        p.JobTitle == m_Person.JobTitle),
                                                     It.Is<string>(i => i == m_Person.Id.ToString()),
                                                     It.Is<PartitionKey>(p => p.Equals(new PartitionKey(m_SearchFirmId.ToString()))),
                                                     It.IsAny<ItemRequestOptions>(),
                                                     It.IsAny<CancellationToken>()));

        }

        [Theory]
        [InlineData("skill")]
        [InlineData("does not exist")]
        public async Task DeleteKeywordsThrowsExceptionWhenKeywordNotExist(string missingKeyword)
        {
            // Given
            var controller = CreateController();
            m_Command = new Delete.Command { Keyword = missingKeyword };

            // When
            var ex = await Record.ExceptionAsync(() => controller.Delete(m_Person.Id, m_Command));

            // Then
            Assert.NotNull(ex);
            Assert.IsType<ResourceNotFoundException>(ex);
        }

        [Fact]
        public async Task DeleteThrowsResourceNotFoundIfNoPerson()
        {
            // Given
            var controller = CreateController();
            m_Command = new Delete.Command { Keyword = _KEYWORD1 };

            // When
            var ex = await Record.ExceptionAsync(() => controller.Delete(m_MissingPersonId, m_Command));

            // Then
            Assert.NotNull(ex);
            Assert.IsType<ResourceNotFoundException>(ex);
        }


        private KeywordsController CreateController()
         => new ControllerBuilder<KeywordsController>()
           .SetFakeCosmos(m_FakeCosmos)
           .SetSearchFirmUser(m_SearchFirmId)
           .SetFakeRepository(new FakeRepository())
           .Build();
    }
}
