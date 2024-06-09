using Ikiru.Parsnips.Api.Controllers.Persons.GdprLawfulBasis;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ResourceNotFound;
using Ikiru.Parsnips.UnitTests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Moq;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Persons.GdprLawfulBasis
{
    public class PutTests
    {
        private readonly FakeCosmos m_FakeCosmos;
        private readonly Guid m_SearchFirmId = Guid.NewGuid();

        private readonly Person m_Person;
        private readonly Put.Command m_Command;

        private readonly Guid m_MissingPersonId = Guid.NewGuid();
        private readonly Guid m_PersonIdThrowInternalServerError = Guid.NewGuid();

        public PutTests()
        {
            m_Person = new Person(m_SearchFirmId);

            m_FakeCosmos = new FakeCosmos()
                          .EnableContainerFetchThrowCosmosException<Person>(FakeCosmos.PersonsContainerName, m_PersonIdThrowInternalServerError.ToString(), m_SearchFirmId.ToString(), HttpStatusCode.InternalServerError)
                          .EnableContainerFetchThrowCosmosException<Person>(FakeCosmos.PersonsContainerName, m_MissingPersonId.ToString(), m_SearchFirmId.ToString(), HttpStatusCode.NotFound)
                          .EnableContainerFetch(FakeCosmos.PersonsContainerName, m_Person.Id.ToString(), m_SearchFirmId.ToString(), () => m_Person)
                          .EnableContainerReplace<Person>(FakeCosmos.PersonsContainerName, m_Person.Id.ToString(), m_SearchFirmId.ToString());
            
            m_Command = new Put.Command
                        {
                            GdprLawfulBasisState = new Put.GdprLawfulBasisState
                                                   {
                                                       GdprDataOrigin = "data stored according to the person's verbal consent",
                                                       GdprLawfulBasisOptionsStatus = GdprLawfulBasisOptionsStatusEnum.ConsentGiven,
                                                       GdprLawfulBasisOption = GdprLawfulBasisOptionEnum.DigitalConsent
                                                   }
                        };
        }


        [Fact]
        public async Task PutUpdatesItemInContainer()
        {
            // Given
            var controller = CreateController();

            // When
            var actionResult = await controller.Put(m_Person.Id, m_Command);

            // Then
            Assert.IsType<NoContentResult>(actionResult);
            var container = m_FakeCosmos.PersonsContainer;

            container.Verify(c => c.ReplaceItemAsync(It.IsAny<Person>(), It.IsAny<string>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()), Times.Once);
            container.Verify(x => x.ReplaceItemAsync(It.IsAny<Person>(), It.IsAny<string>(), It.Is<PartitionKey>(p => p == new PartitionKey(m_SearchFirmId.ToString())), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.ReplaceItemAsync(It.IsAny<Person>(), It.Is<string>(id => id == m_Person.Id.ToString()), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(p => p.Id == m_Person.Id), It.IsAny<string>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(p => p.GdprLawfulBasisState.GdprLawfulBasisOptionsStatus ==
                                                                                   m_Command.GdprLawfulBasisState.GdprLawfulBasisOptionsStatus), It.IsAny<string>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(p => p.GdprLawfulBasisState.GdprLawfulBasisOption ==
                                                                                   m_Command.GdprLawfulBasisState.GdprLawfulBasisOption), It.IsAny<string>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(p => p.GdprLawfulBasisState.GdprDataOrigin == m_Command.GdprLawfulBasisState.GdprDataOrigin), It.IsAny<string>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
        }


        [Fact]
        public async Task PutDoesNotUpdateItemInContainerIfGdprNull()
        {
            // Given 
            m_Command.GdprLawfulBasisState = null;
            var controller = CreateController();

            // When
            var actionResult = await controller.Put(m_Person.Id, m_Command);

            // Then
            Assert.IsType<NoContentResult>(actionResult);
            var container = m_FakeCosmos.PersonsContainer;

            container.Verify(c => c.ReplaceItemAsync(It.IsAny<Person>(), It.IsAny<string>(), It.IsAny<PartitionKey>(),
                                                     It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()), Times.Never);

        }

        [Fact]
        public async Task PutThrowsExceptionIfPersonNotFound()
        {
            // Given
            var controller = CreateController();

            // When
            var result = await Record.ExceptionAsync(() => controller.Put(m_MissingPersonId, m_Command));

            // Then
            Assert.IsType<ResourceNotFoundException>(result);
        }

        [Fact]
        public async Task PutThrowsExceptionIfCosmosThrowsInternalServerError()
        {
            // Given
            var controller = CreateController();

            // When
            var result = await Record.ExceptionAsync(() => controller.Put(m_PersonIdThrowInternalServerError, m_Command));

            // Then
            Assert.IsType<CosmosException>(result);
        }

        private GdprLawfulBasisController CreateController()
        {
            return new ControllerBuilder<GdprLawfulBasisController>()
                  .SetFakeCosmos(m_FakeCosmos)
                  .SetSearchFirmUser(m_SearchFirmId)
                  .SetFakeRepository(new FakeRepository())
                  .Build();
        }
    }
}
