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
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Persons.Keywords
{
    public class PostTests
    {
        private const string _KEYWORD = "Personal skills";
        private const string _ANOTHER_KEYWORD = "Determination";

        private readonly FakeCosmos m_FakeCosmos;
        private readonly Post.Command m_Command;
        private readonly Person m_Person;

        private readonly Guid m_SearchFirmId = Guid.NewGuid();

        private readonly Guid m_MissingPersonId = Guid.NewGuid();


        public PostTests()
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

            m_Command = new Post.Command { Keyword = _KEYWORD };
        }

        [Fact]
        public async Task PostReturnsNoContent()
        {
            // Given
            var controller = CreateController();

            // When
            var actionResult = await controller.Post(m_Person.Id, m_Command);

            // Then
            Assert.True(actionResult is NoContentResult);
        }

        [Fact]
        public async Task PostAddsKeyword()
        {
            // Given
            var controller = CreateController();

            // When
            await controller.Post(m_Person.Id, m_Command);

            // Then
            var container = m_FakeCosmos.PersonsContainer;
            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(p => p.Keywords.Single() == _KEYWORD), It.Is<string>(i => i == m_Person.Id.ToString()), It.Is<PartitionKey>(p => p.Equals(new PartitionKey(m_SearchFirmId.ToString()))), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));

            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(p => p.Id == m_Person.Id), It.Is<string>(i => i == m_Person.Id.ToString()), It.Is<PartitionKey>(p => p.Equals(new PartitionKey(m_SearchFirmId.ToString()))), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(p => p.CreatedDate == m_Person.CreatedDate), It.Is<string>(i => i == m_Person.Id.ToString()), It.Is<PartitionKey>(p => p.Equals(new PartitionKey(m_SearchFirmId.ToString()))), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(p => p.Name == m_Person.Name), It.Is<string>(i => i == m_Person.Id.ToString()), It.Is<PartitionKey>(p => p.Equals(new PartitionKey(m_SearchFirmId.ToString()))), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(p => p.Location == m_Person.Location), It.Is<string>(i => i == m_Person.Id.ToString()), It.Is<PartitionKey>(p => p.Equals(new PartitionKey(m_SearchFirmId.ToString()))), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(p => p.TaggedEmails.AssertSameList(m_Person.TaggedEmails)), It.Is<string>(i => i == m_Person.Id.ToString()), It.Is<PartitionKey>(p => p.Equals(new PartitionKey(m_SearchFirmId.ToString()))), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(p => p.PhoneNumbers.IsSameList(m_Person.PhoneNumbers)), It.Is<string>(i => i == m_Person.Id.ToString()), It.Is<PartitionKey>(p => p.Equals(new PartitionKey(m_SearchFirmId.ToString()))), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(p => p.JobTitle == m_Person.JobTitle), It.Is<string>(i => i == m_Person.Id.ToString()), It.Is<PartitionKey>(p => p.Equals(new PartitionKey(m_SearchFirmId.ToString()))), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(p => p.Organisation == m_Person.Organisation), It.Is<string>(i => i == m_Person.Id.ToString()), It.Is<PartitionKey>(p => p.Equals(new PartitionKey(m_SearchFirmId.ToString()))), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(p => p.LinkedInProfileUrl == m_Person.LinkedInProfileUrl), It.Is<string>(i => i == m_Person.Id.ToString()), It.Is<PartitionKey>(p => p.Equals(new PartitionKey(m_SearchFirmId.ToString()))), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(p => p.LinkedInProfileId == m_Person.LinkedInProfileId), It.Is<string>(i => i == m_Person.Id.ToString()), It.Is<PartitionKey>(p => p.Equals(new PartitionKey(m_SearchFirmId.ToString()))), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(p => p.SearchFirmId == m_Person.SearchFirmId), It.Is<string>(i => i == m_Person.Id.ToString()), It.Is<PartitionKey>(p => p.Equals(new PartitionKey(m_SearchFirmId.ToString()))), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(p => p.GdprLawfulBasisState.GdprLawfulBasisOptionsStatus == m_Person.GdprLawfulBasisState.GdprLawfulBasisOptionsStatus), It.Is<string>(i => i == m_Person.Id.ToString()), It.Is<PartitionKey>(p => p.Equals(new PartitionKey(m_SearchFirmId.ToString()))), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(p => p.GdprLawfulBasisState.GdprLawfulBasisOption == m_Person.GdprLawfulBasisState.GdprLawfulBasisOption), It.Is<string>(i => i == m_Person.Id.ToString()), It.Is<PartitionKey>(p => p.Equals(new PartitionKey(m_SearchFirmId.ToString()))), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(p => p.GdprLawfulBasisState.GdprDataOrigin == m_Person.GdprLawfulBasisState.GdprDataOrigin), It.Is<string>(i => i == m_Person.Id.ToString()), It.Is<PartitionKey>(p => p.Equals(new PartitionKey(m_SearchFirmId.ToString()))), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task PostAllowsDuplicateKeyword()
        {
            // Given
            var controller = CreateController();

            // When
            await controller.Post(m_Person.Id, m_Command);
            await controller.Post(m_Person.Id, m_Command);

            // Then
            var container = m_FakeCosmos.PersonsContainer;
            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(p => p.Keywords.Any(k => k == _KEYWORD)), It.Is<string>(i => i == m_Person.Id.ToString()), It.Is<PartitionKey>(p => p.Equals(new PartitionKey(m_SearchFirmId.ToString()))), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task PostAllowsMultipleKeywords()
        {
            // Given
            var controller = CreateController();

            // When
            await controller.Post(m_Person.Id, m_Command);
            await controller.Post(m_Person.Id, new Post.Command { Keyword = _ANOTHER_KEYWORD });

            // Then
            var container = m_FakeCosmos.PersonsContainer;
            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(p => p.Keywords.Any(k => k == _KEYWORD)), It.Is<string>(i => i == m_Person.Id.ToString()), It.Is<PartitionKey>(p => p.Equals(new PartitionKey(m_SearchFirmId.ToString()))), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(p => p.Keywords.Any(k => k == _ANOTHER_KEYWORD)), It.Is<string>(i => i == m_Person.Id.ToString()), It.Is<PartitionKey>(p => p.Equals(new PartitionKey(m_SearchFirmId.ToString()))), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
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
            var expectedException = new CosmosException("Not authorised", HttpStatusCode.Unauthorized, 9, "activity-2", 0);
            m_FakeCosmos.PersonsContainer
                        .Setup(c => c.ReadItemAsync<Person>(It.Is<string>(i => i == personId.ToString()),
                            It.Is<PartitionKey>(p => p.Equals(new PartitionKey(m_SearchFirmId.ToString()))),
                            It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                        .ThrowsAsync(expectedException);
            var controller = CreateController();

            // When
            var ex = await Record.ExceptionAsync(() => controller.Post(personId, m_Command));

            // Then
            Assert.NotNull(ex);
            Assert.Same(expectedException, ex);
        }


        private KeywordsController CreateController()
          => new ControllerBuilder<KeywordsController>()
            .SetFakeCosmos(m_FakeCosmos)
            .SetSearchFirmUser(m_SearchFirmId)
            .SetFakeRepository(new FakeRepository())
            .Build();
    }
}
