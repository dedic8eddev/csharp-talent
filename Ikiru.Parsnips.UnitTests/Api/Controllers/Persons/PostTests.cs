using Ikiru.Parsnips.Api.Controllers.Persons;
using Ikiru.Parsnips.Application.Infrastructure.DataPool;
using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues.Items;
using Ikiru.Parsnips.UnitTests.Helpers;
using Ikiru.Parsnips.UnitTests.Helpers.TestDataSources;
using Ikiru.Parsnips.UnitTests.Helpers.Validation;
using Ikiru.Persistence.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Persons
{
    public class PostTests
    {
        private readonly Guid m_SearchFirmId = Guid.NewGuid();
        private readonly Guid m_DataPoolPersonId = Guid.NewGuid();
        private Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi.Models.Person.Person m_DataPoolPerson;
        private Person m_LocalPerson;

        private readonly Mock<IRepository> m_RepositoryMock = new Mock<IRepository>();
        private readonly Mock<IPersonInfrastructure> m_PersonInfrastructure = new Mock<IPersonInfrastructure>();

        private readonly List<Person> m_StoredPersons = new List<Person>();

        private readonly Post.Command m_Command = new Post.Command
        {
            Name = "Gruff Rhys",
            JobTitle = "Lead Singer",
            Location = "Haverfordwest, Pembrokeshire, Wales",
            TaggedEmails = new List<Post.Command.TaggedEmail> { new Post.Command.TaggedEmail { Email = "gruff@gruffrhys.com" }, new Post.Command.TaggedEmail { Email = "band@superfurry.com" } },
            PhoneNumbers = new List<string> { "01234 567890", "00000 333333" },
            Company = "Super Furry Animals",
            LinkedInProfileUrl = "https://uk.linkedin.com/in/gruffrhys"
        };

        private readonly FakeCosmos m_FakeCosmos;
        private readonly FakeStorageQueue m_FakeStorageQueue = new FakeStorageQueue();

        public PostTests()
        {
            m_LocalPerson = new Person(m_SearchFirmId);

            m_DataPoolPerson = new Shared.Infrastructure.DataPoolApi.Models.Person.Person
            {
                Id = m_DataPoolPersonId
            };

            m_LocalPerson.DataPoolPersonId = m_DataPoolPersonId;

            m_FakeCosmos = new FakeCosmos()
                          .EnableContainerInsert<Person>(FakeCosmos.PersonsContainerName)
                          .EnableContainerLinqQuery(FakeCosmos.PersonsContainerName, m_SearchFirmId.ToString(), () => m_StoredPersons);
        }

        [Fact]
        public async Task PostCreatesItemInContainer()
        {
            // Given
            var controller = CreateController();

            // When
            var actionResult = await controller.Post(m_Command);

            // Then
            var container = m_FakeCosmos.PersonsContainer;
            container.Verify(c => c.CreateItemAsync(It.IsAny<Person>(), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()), Times.Once);
            container.Verify(c => c.CreateItemAsync(It.IsAny<Person>(), It.Is<PartitionKey?>(p => p == new PartitionKey(m_SearchFirmId.ToString())), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            var result = (Post.Result)((CreatedAtActionResult)actionResult).Value;
            container.Verify(c => c.CreateItemAsync(It.Is<Person>(i => i.Id == result.Id), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.CreateItemAsync(It.Is<Person>(i => i.CreatedDate.Date == DateTime.UtcNow.Date), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.CreateItemAsync(It.Is<Person>(i => i.SearchFirmId == m_SearchFirmId), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.CreateItemAsync(It.Is<Person>(i => i.Name == m_Command.Name), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.CreateItemAsync(It.Is<Person>(i => i.JobTitle == m_Command.JobTitle), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.CreateItemAsync(It.Is<Person>(i => i.Location == m_Command.Location), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.CreateItemAsync(It.Is<Person>(i => i.TaggedEmails.AssertSameList(m_Command.TaggedEmails)), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.CreateItemAsync(It.Is<Person>(i => i.PhoneNumbers.IsSameList(m_Command.PhoneNumbers)), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.CreateItemAsync(It.Is<Person>(i => i.Organisation == m_Command.Company), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.CreateItemAsync(It.Is<Person>(i => i.LinkedInProfileUrl == m_Command.LinkedInProfileUrl), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
        }

        [Theory]
        [ClassData(typeof(ValidLinkedInProfileUrlNormalisationsIncludingRedirect))]
        public async Task PostCreatesItemInContainerWithNormalisedProfileId(string profileUrl, string expectedNormalisedProfileId)
        {
            // Given
            m_Command.LinkedInProfileUrl = profileUrl;
            var controller = CreateController();

            // When
            await controller.Post(m_Command);

            // Then
            var container = m_FakeCosmos.PersonsContainer;
            container.Verify(c => c.CreateItemAsync(It.Is<Person>(i => i.LinkedInProfileId == expectedNormalisedProfileId), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
        }

        [Theory]
        [ClassData(typeof(UnpopulatedStrings))]
        public async Task PostCreatesItemInContainerWithNormalisedProfileIdWhenEmptyProfileUrl(string emptyProfileUrl)
        {
            // Given
            m_Command.LinkedInProfileUrl = emptyProfileUrl;
            var controller = CreateController();

            // When
            await controller.Post(m_Command);

            // Then
            var container = m_FakeCosmos.PersonsContainer;
            Guid unused;
            container.Verify(c => c.CreateItemAsync(It.Is<Person>(i => i.LinkedInProfileId.StartsWith("Empty-") && Guid.TryParse(i.LinkedInProfileId.Replace("Empty-", ""), out unused)),
                                                    It.IsAny<PartitionKey?>(),
                                                    It.IsAny<ItemRequestOptions>(),
                                                    It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task PostQueuesChangedLocationMessage()
        {
            // Given
            var controller = CreateController();

            // When
            var actionResult = await controller.Post(m_Command);

            // Then
            var queuedItem = m_FakeStorageQueue.GetQueuedItem<PersonLocationChangedQueueItem>(QueueStorage.QueueNames.PersonLocationChangedQueue);
            var result = (Post.Result)((CreatedAtActionResult)actionResult).Value;
            Assert.Equal(result.Id, queuedItem.PersonId);
            Assert.Equal(m_SearchFirmId, queuedItem.SearchFirmId);
        }

        [Theory]
        [ClassData(typeof(UnpopulatedStrings))]
        public async Task PostDoesNotQueueChangedLocationMessageIfLocationEmpty(string location)
        {
            // Given
            m_Command.Location = location;
            var controller = CreateController();

            // When
            await controller.Post(m_Command);

            // Then
            Assert.Equal(0, m_FakeStorageQueue.GetQueuedItemCount(QueueStorage.QueueNames.PersonLocationChangedQueue));
        }

        [Fact]
        public async Task PostReturnsCorrectResult()
        {
            // Given
            var controller = CreateController();

            // When
            var actionResult = await controller.Post(m_Command);

            // Then
            var createdActionResult = (CreatedAtActionResult)actionResult;
            var result = (Post.Result)createdActionResult.Value;

            Assert.Equal("Get", createdActionResult.ActionName);
            Assert.Contains("Id", createdActionResult.RouteValues.Keys);
            Assert.Equal(result.Id, createdActionResult.RouteValues["Id"]);

            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Equal(m_Command.Name, result.Name);
            Assert.Equal(m_Command.JobTitle, result.JobTitle);
            Assert.Equal(m_Command.Location, result.Location);
            Assert.All(m_Command.TaggedEmails, e => Assert.Contains(result.TaggedEmails, tr => e.Email == tr.Email && e.SmtpValid == tr.SmtpValid));
            Assert.Equal(m_Command.PhoneNumbers, result.PhoneNumbers);
            Assert.Equal(m_Command.Company, result.Company);
            Assert.Equal(m_Command.LinkedInProfileUrl, result.LinkedInProfileUrl);
        }

        [Theory]
        [ClassData(typeof(ValidLinkedInProfileUrlNormalisationsIncludingRedirect))]
        public async Task PostThrowsWhenPersonExistsWithSameProfileId(string requestProfileId, string existingRecordProfileId)
        {
            // Given
            m_Command.LinkedInProfileUrl = requestProfileId;
            m_StoredPersons.Add(new Person(m_SearchFirmId, linkedInProfileUrl: $"https://www.linkedin.com/in/{existingRecordProfileId}"));
            var controller = CreateController();

            // When
            var ex = await Record.ExceptionAsync(() => controller.Post(m_Command));

            // Then
            Assert.NotNull(ex);
            var vex = Assert.IsType<ParamValidationFailureException>(ex);
            var validationError = vex.ValidationErrors.Where(e => e.Param == nameof(Post.Command.LinkedInProfileUrl)).ToList();
            Assert.Single(validationError);
            var paramValidationError = validationError.Single();
            Assert.Single(paramValidationError.Errors);
            Assert.Equal("A record already exists with this {Param}", paramValidationError.Errors.Single());
        }

        private PersonsController CreateController()
        {
            return new ControllerBuilder<PersonsController>()
                  .SetFakeCosmos(m_FakeCosmos)
                  .SetFakeCloudQueue(m_FakeStorageQueue)
                  .SetSearchFirmUser(m_SearchFirmId)
                   .AddTransient(m_RepositoryMock.Object)
                    .AddTransient(m_PersonInfrastructure.Object)
                  .Build();
        }
    }
}
