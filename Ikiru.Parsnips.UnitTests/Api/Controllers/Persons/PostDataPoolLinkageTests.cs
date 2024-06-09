using Ikiru.Parsnips.Api.Controllers.Persons;
using Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.UnitTests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using DataPoolApiModel = Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi.Models;
using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Application.Infrastructure.DataPool;
using Ikiru.Persistence.Repository;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Persons
{
    public class PostDataPoolLinkageTests
    {
        private readonly Mock<IDataPoolApi> m_DataPoolApi;
        private readonly Guid m_SearchFirmId = Guid.NewGuid();
        private readonly Guid m_DataPoolPersonId = Guid.NewGuid();
        private readonly List<DataPoolApiModel.Person.Person> m_DataPoolPersonList;
        private readonly Person m_LocalPerson;

        private readonly List<Person> m_StoredPersons = new List<Person>();

        private PostDataPoolLinkage.Command m_PostDataPoolLinkageCommand;

        private readonly FakeCosmos m_FakeCosmos;
        private readonly FakeStorageQueue m_FakeStorageQueue = new FakeStorageQueue();
        private readonly Mock<IRepository> m_RepositoryMock = new Mock<IRepository>();
        private readonly Mock<IPersonInfrastructure> m_PersonInfrastructure = new Mock<IPersonInfrastructure>();

        public PostDataPoolLinkageTests()
        {
            m_LocalPerson = new Person(m_SearchFirmId);

            m_DataPoolPersonList = new List<DataPoolApiModel.Person.Person>();

            m_DataPoolApi = new Mock<IDataPoolApi>();

            var dataPoolPerson = new DataPoolApiModel.Person.Person
            {
                Id = m_DataPoolPersonId,
                PersonDetails = new DataPoolApiModel.Person.PersonDetails
                {
                    Name = "JOhn Smith"
                }
            };

            m_DataPoolPersonList.Add(dataPoolPerson);

            m_LocalPerson.DataPoolPersonId = m_DataPoolPersonId;

            m_DataPoolApi.Setup(d => d.Get(It.Is<string>(id => id == m_DataPoolPersonId.ToString()), It.IsAny<CancellationToken>()))
                        .Returns(Task.FromResult(m_DataPoolPersonList.First()));


            m_FakeCosmos = new FakeCosmos()
                          .EnableContainerInsert<Person>(FakeCosmos.PersonsContainerName)
                          .EnableContainerLinqQuery(FakeCosmos.PersonsContainerName, m_SearchFirmId.ToString(), () => m_StoredPersons);
        }

        [Fact]
        public async Task PostLinkageDataPoolCreateDatapoolLinkForNewPerson()
        {
            // Given            
            m_PostDataPoolLinkageCommand = new PostDataPoolLinkage.Command()
            {
                DataPoolPersonId = m_DataPoolPersonId
            };

            var controller = CreateController();

            // When
            var actionResult = await controller.DataPoolLinkage(m_PostDataPoolLinkageCommand);

            // Then
            var createdActionResult = (OkObjectResult)actionResult;
            var result = (PostDataPoolLinkage.Result)createdActionResult.Value;

            m_FakeCosmos.PersonsContainer.Setup(x => x.GetItemLinqQueryable<Person>(It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<QueryRequestOptions>(), It.IsAny<CosmosLinqSerializerOptions>()))
                                         .Returns<Person>(default);

            m_FakeCosmos.PersonsContainer.Verify(x => x.CreateItemAsync(It.Is<Person>(p => p.DataPoolPersonId == m_DataPoolPersonId &&
                                                                                        p.Name == m_DataPoolPersonList[0].PersonDetails.Name &&
                                                                                      p.Id != Guid.Empty),
                                                                        It.Is<PartitionKey>(p => p == new PartitionKey(m_SearchFirmId.ToString())),
                                                                        It.IsAny<ItemRequestOptions>(),
                                                                        It.IsAny<CancellationToken>()), Times.Once);

            Assert.Equal(m_DataPoolPersonId, result.LocalPerson.DataPoolPersonId);
            Assert.Equal(m_DataPoolPersonId, result.DataPoolPerson.Id);
        }

        [Fact]
        public async Task PostLinkageDataPoolExistingPerson()
        {
            // Given            
            var controller = CreateController();
            m_PostDataPoolLinkageCommand = new PostDataPoolLinkage.Command()
            {
                DataPoolPersonId = m_DataPoolPersonId
            };

            var persons = new List<Person>();
            persons.Add(m_LocalPerson);
            m_FakeCosmos.PersonsContainer.Setup(x => x.GetItemLinqQueryable<Person>(It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<QueryRequestOptions>(), It.IsAny<CosmosLinqSerializerOptions>()))
                                      .Returns(persons.AsMockedOrderedQueryable());

            // When
            var actionResult = await controller.DataPoolLinkage(m_PostDataPoolLinkageCommand);

            // Then
            var createdActionResult = (OkObjectResult)actionResult;
            var result = (PostDataPoolLinkage.Result)createdActionResult.Value;

            m_FakeCosmos.PersonsContainer.Verify(x => x.CreateItemAsync(It.Is<Person>(p => p.DataPoolPersonId == m_DataPoolPersonId &&
                                                                                        p.Name == m_DataPoolPersonList[0].PersonDetails.Name &&
                                                                                      p.Id != Guid.Empty),
                                                                        It.Is<PartitionKey>(p => p == new PartitionKey(m_SearchFirmId.ToString())),
                                                                        It.IsAny<ItemRequestOptions>(),
                                                                        It.IsAny<CancellationToken>()), Times.Never);

            Assert.Equal(m_DataPoolPersonId, result.LocalPerson.DataPoolPersonId);
            Assert.Equal(m_DataPoolPersonId, result.DataPoolPerson.DataPoolPersonId);
        }


        [Fact]
        public async Task PostLinkageDataPoolGetPersonDetailsFromDatapool()
        {
            // Given            
            var controller = CreateController();
            m_PostDataPoolLinkageCommand = new PostDataPoolLinkage.Command()
            {
                DataPoolPersonId = m_DataPoolPersonId
            };

            var persons = new List<Person>();
            persons.Add(m_LocalPerson);

            m_FakeCosmos.PersonsContainer.Setup(x => x.GetItemLinqQueryable<Person>(It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<QueryRequestOptions>(), It.IsAny<CosmosLinqSerializerOptions>()))
                                      .Returns(persons.AsMockedOrderedQueryable());

            // When
            var actionResult = await controller.DataPoolLinkage(m_PostDataPoolLinkageCommand);

            // Then
            var createdActionResult = (OkObjectResult)actionResult;
            var result = (PostDataPoolLinkage.Result)createdActionResult.Value;

            m_DataPoolApi.Verify(x => x.Get(It.Is<string>(x => x == m_DataPoolPersonId.ToString()), It.IsAny<CancellationToken>()), Times.Once);

        }

        private PersonsController CreateController()
        {
            return new ControllerBuilder<PersonsController>()
                  .SetFakeCosmos(m_FakeCosmos)
                  .SetFakeCloudQueue(m_FakeStorageQueue)
                  .SetSearchFirmUser(m_SearchFirmId)
                     .AddTransient(m_RepositoryMock.Object)
                    .AddTransient(m_PersonInfrastructure.Object)
                  .AddTransient(m_DataPoolApi.Object)
                  .Build();
        }
    }
}
