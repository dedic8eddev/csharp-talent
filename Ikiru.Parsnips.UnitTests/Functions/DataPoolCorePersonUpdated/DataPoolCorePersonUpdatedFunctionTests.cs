using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.Functions.Functions.DataPoolCorePersonUpdated;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues.Items;
using Ikiru.Parsnips.UnitTests.Helpers;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Storage.Queue;
using Moq;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues;
using Ikiru.Parsnips.UnitTests.Helpers.TestDataSources;
using Ikiru.Parsnips.UnitTests.Helpers.Validation;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Functions.Functions.DataPoolCorePersonUpdated
{
    public class DataPoolCorePersonUpdatedFunctionTests
    {
        private readonly Guid m_SearchFirmId = Guid.NewGuid();
        private readonly Guid m_LinkedPersonDataPoolPersonId = Guid.NewGuid();

        private readonly Person m_LinkedPerson;
        private readonly Person m_UnlinkedPersonOne;

        private const string _UNLINKED_PERSON_ONE_PROFILE_ID = "unlinked-one";
        private const string _LINKED_PERSON_PROFILE_ID = "linked-person";

        private readonly DataPoolCorePersonUpdatedQueueItem m_QueueItem;
        private CloudQueueMessage QueueMessage() => new CloudQueueMessage(JsonSerializer.Serialize(m_QueueItem));
        
        private readonly FakeCosmos m_FakeCosmos;
        private readonly FakeStorageQueue m_FakeStorageQueue = new FakeStorageQueue();

        public DataPoolCorePersonUpdatedFunctionTests()
        {
            m_LinkedPerson = new Person(m_SearchFirmId, Guid.NewGuid(), $"https://uk.linkedin.com/in/{_LINKED_PERSON_PROFILE_ID}")
                             {
                                 Name = "Gruff Rhys",
                                 JobTitle = "Lead Singer",
                                 Location = "Haverfordwest, Pembrokeshire, Wales",
                                 Organisation = "Super Furry Animals",

                                 GdprLawfulBasisState = new PersonGdprLawfulBasisState
                                                        {
                                                            GdprDataOrigin = "Some Bloke",
                                                            GdprLawfulBasisOptionsStatus = GdprLawfulBasisOptionsStatusEnum.NotificationSent,
                                                            GdprLawfulBasisOption = GdprLawfulBasisOptionEnum.LegitimateInterest
                                                        },
                                 PhoneNumbers = new List<string> { "01234 5678900", "11111, 233324" },
                                 TaggedEmails = new List<TaggedEmail> { new TaggedEmail { Email = "gruff@gruffrhys.com" }, new TaggedEmail { Email = "band@superfurry.com" } },

                                 Keywords = new List<string>
                                            {
                                                "Key",
                                                "Word"
                                            },
                                 Documents = new List<PersonDocument>
                                             {
                                                 new PersonDocument(m_SearchFirmId, "test.pdf"),
                                                 new PersonDocument(m_SearchFirmId, "file.docx")
                                             },

                                 SectorsIds = new List<string> { "I16721", "I150" }
                             };
            m_LinkedPerson.SetDataPoolPersonId(m_LinkedPersonDataPoolPersonId);

            m_UnlinkedPersonOne = new Person(m_SearchFirmId, Guid.NewGuid(), $"https://uk.linkedin.com/in/{_UNLINKED_PERSON_ONE_PROFILE_ID}")
                       {
                           Name = "Davey Crockett",
                           JobTitle = "Ghost",
                           Location = "San Antonio",
                           Organisation = "Mexican Freedom",

                           GdprLawfulBasisState = new PersonGdprLawfulBasisState
                                                  {
                                                      GdprDataOrigin = "History",
                                                      GdprLawfulBasisOptionsStatus = GdprLawfulBasisOptionsStatusEnum.ConsentGiven,
                                                      GdprLawfulBasisOption = GdprLawfulBasisOptionEnum.EmailConsent
                                                  },
                           PhoneNumbers = new List<string> { "01234 999754", },
                           TaggedEmails = new List<TaggedEmail> { new TaggedEmail { Email = "davey@alamo.com" }, new TaggedEmail { Email = "dcrockett@sanantonio.com" } },

                           Keywords = new List<string>
                                      {
                                          "Battle",
                                          "Mexico"
                                      },
                           Documents = new List<PersonDocument>
                                       {
                                           new PersonDocument(m_SearchFirmId, "alamo.pdf")
                                       },

                           SectorsIds = new List<string> { "I167" }
                       };
            
            var unlinkedPersonTwo = new Person(m_SearchFirmId, Guid.NewGuid(), "https://uk.linkedin.com/in/unlinked-two")
                                      {
                                          Name = "Unlinked Two"
                                      };

            m_FakeCosmos = new FakeCosmos()
                          .EnableContainerLinqQuery(FakeCosmos.PersonsContainerName, m_SearchFirmId.ToString(), () => new List<Person> { m_LinkedPerson, m_UnlinkedPersonOne, unlinkedPersonTwo })
                          .EnableContainerFetch(FakeCosmos.PersonsContainerName, m_LinkedPerson.Id.ToString(), m_SearchFirmId.ToString(), () => m_LinkedPerson)
                          .EnableContainerFetch(FakeCosmos.PersonsContainerName, m_UnlinkedPersonOne.Id.ToString(), m_SearchFirmId.ToString(), () => m_UnlinkedPersonOne)
                          .EnableContainerReplace<Person>(FakeCosmos.PersonsContainerName, m_LinkedPerson.Id.ToString(), m_SearchFirmId.ToString())
                          .EnableContainerReplace<Person>(FakeCosmos.PersonsContainerName, m_UnlinkedPersonOne.Id.ToString(), m_SearchFirmId.ToString());

            m_QueueItem = new DataPoolCorePersonUpdatedQueueItem
                          {
                              SearchFirmId = m_SearchFirmId,
                              CorePerson = new DataPoolCorePersonUpdatedQueueItem.DataPoolCorePersonUpdated
                                           {
                                               DataPoolPersonId = m_LinkedPersonDataPoolPersonId, // Default - Same DP Person Id as Linked Person

                                               FirstName = "Updatey",
                                               LastName = "McUpdated",
                                               JobTitle = "Updater Extraordinaire",
                                               Company = "Update Inc.",
                                               Location = "Updatesville",
                                               LinkedInProfileUrl = $"https://uk.linkedin.com/in/{_UNLINKED_PERSON_ONE_PROFILE_ID}", 
                                               LinkedInProfileId = _UNLINKED_PERSON_ONE_PROFILE_ID // Default - Same LI ID as First Unlinked Person
                                           }
                          };
        }
        
        [Fact]
        public async Task FunctionUpdatesCorePersonDataIfMatchedDataPoolPersonId()
        {
            // Given
            var function = CreateFunction();

            // When
            await function.Run(QueueMessage(), Mock.Of<ILogger>());

            // Then
            var container = m_FakeCosmos.PersonsContainer;

            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(p => p.Id == m_LinkedPerson.Id &&
                                                                        p.DataPoolPersonId == m_LinkedPersonDataPoolPersonId &&
                                                                        p.SearchFirmId == m_SearchFirmId &&
                                                                        p.CreatedDate == m_LinkedPerson.CreatedDate &&
                                                                        p.ImportId == m_LinkedPerson.ImportId &&
                                                                        p.LinkedInProfileUrl == m_LinkedPerson.LinkedInProfileUrl &&
                                                                        p.LinkedInProfileId == m_LinkedPerson.LinkedInProfileId &&
                                                                        p.ImportedLinkedInProfileUrl == m_LinkedPerson.ImportedLinkedInProfileUrl &&
                                                                        p.Name == $"{m_QueueItem.CorePerson.FirstName} {m_QueueItem.CorePerson.LastName}" &&
                                                                        p.Location == m_QueueItem.CorePerson.Location &&
                                                                        p.Organisation == m_QueueItem.CorePerson.Company &&
                                                                        p.JobTitle == m_QueueItem.CorePerson.JobTitle &&
                                                                        p.PhoneNumbers.IsSameList(m_LinkedPerson.PhoneNumbers) &&
                                                                        p.TaggedEmails.AssertSameList(m_LinkedPerson.TaggedEmails) &&
                                                                        p.Keywords.IsSameList(m_LinkedPerson.Keywords) &&
                                                                        p.GdprLawfulBasisState.GdprDataOrigin == m_LinkedPerson.GdprLawfulBasisState.GdprDataOrigin &&
                                                                        p.GdprLawfulBasisState.GdprLawfulBasisOption == m_LinkedPerson.GdprLawfulBasisState.GdprLawfulBasisOption &&
                                                                        p.GdprLawfulBasisState.GdprLawfulBasisOptionsStatus == m_LinkedPerson.GdprLawfulBasisState.GdprLawfulBasisOptionsStatus &&
                                                                        p.Documents.IsSameList(m_LinkedPerson.Documents) &&
                                                                        p.SectorsIds.IsSameList(m_LinkedPerson.SectorsIds)
                                                                  ), 
                                                     It.Is<string>(id => id == m_LinkedPerson.Id.ToString()), 
                                                     It.Is<PartitionKey>(p => p == new PartitionKey(m_SearchFirmId.ToString())),
                                                     It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
        }
        
        [Fact]
        public async Task FunctionUpdatesCorePersonDataIfDoesNotMatchDataPoolPersonIdButMatchesLinkedInProfileId()
        {
            // Given
            m_QueueItem.CorePerson.DataPoolPersonId = Guid.NewGuid();
            var function = CreateFunction();

            // When
            await function.Run(QueueMessage(), Mock.Of<ILogger>());

            // Then
            var container = m_FakeCosmos.PersonsContainer;

            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(p => p.Id == m_UnlinkedPersonOne.Id &&
                                                                        p.DataPoolPersonId == m_QueueItem.CorePerson.DataPoolPersonId &&
                                                                        p.SearchFirmId == m_SearchFirmId &&
                                                                        p.CreatedDate == m_UnlinkedPersonOne.CreatedDate &&
                                                                        p.ImportId == m_UnlinkedPersonOne.ImportId &&
                                                                        p.LinkedInProfileUrl == m_UnlinkedPersonOne.LinkedInProfileUrl &&
                                                                        p.LinkedInProfileId == m_UnlinkedPersonOne.LinkedInProfileId &&
                                                                        p.ImportedLinkedInProfileUrl == m_UnlinkedPersonOne.ImportedLinkedInProfileUrl &&
                                                                        p.Name == $"{m_QueueItem.CorePerson.FirstName} {m_QueueItem.CorePerson.LastName}" &&
                                                                        p.Location == m_QueueItem.CorePerson.Location &&
                                                                        p.Organisation == m_QueueItem.CorePerson.Company &&
                                                                        p.JobTitle == m_QueueItem.CorePerson.JobTitle &&
                                                                        p.PhoneNumbers.IsSameList(m_UnlinkedPersonOne.PhoneNumbers) &&
                                                                        p.TaggedEmails.AssertSameList(m_UnlinkedPersonOne.TaggedEmails) &&
                                                                        p.Keywords.IsSameList(m_UnlinkedPersonOne.Keywords) &&
                                                                        p.GdprLawfulBasisState.GdprDataOrigin == m_UnlinkedPersonOne.GdprLawfulBasisState.GdprDataOrigin &&
                                                                        p.GdprLawfulBasisState.GdprLawfulBasisOption == m_UnlinkedPersonOne.GdprLawfulBasisState.GdprLawfulBasisOption &&
                                                                        p.GdprLawfulBasisState.GdprLawfulBasisOptionsStatus == m_UnlinkedPersonOne.GdprLawfulBasisState.GdprLawfulBasisOptionsStatus &&
                                                                        p.Documents.IsSameList(m_UnlinkedPersonOne.Documents) &&
                                                                        p.SectorsIds.IsSameList(m_UnlinkedPersonOne.SectorsIds)
                                                                  ), 
                                                     It.Is<string>(id => id == m_UnlinkedPersonOne.Id.ToString()), 
                                                     It.Is<PartitionKey>(p => p == new PartitionKey(m_SearchFirmId.ToString())),
                                                     It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
        }
        
        [Fact]
        public async Task FunctionDoesNotUpdatePersonIfMatchOnLinkedInProfileIdButPersonAlreadyHasOtherDataPoolPersonId()
        {
            // Given
            m_QueueItem.CorePerson.DataPoolPersonId = Guid.NewGuid(); // Don't match on DP ID
            m_QueueItem.CorePerson.LinkedInProfileId = _LINKED_PERSON_PROFILE_ID; // Match on LI ID for a Person who is already linked
            var function = CreateFunction();

            // When
            await function.Run(QueueMessage(), Mock.Of<ILogger>());

            // Then
            var container = m_FakeCosmos.PersonsContainer;

            container.Verify(c => c.ReplaceItemAsync(It.IsAny<Person>(), It.IsAny<string>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()),
                             Times.Never);
        }
        
        [Fact]
        public async Task FunctionDoesNotUpdatePersonIfNoMatches()
        {
            // Given
            m_QueueItem.CorePerson.DataPoolPersonId = Guid.NewGuid(); 
            m_QueueItem.CorePerson.LinkedInProfileId = "other-profile-id"; 
            var function = CreateFunction();

            // When
            await function.Run(QueueMessage(), Mock.Of<ILogger>());

            // Then
            var container = m_FakeCosmos.PersonsContainer;

            container.Verify(c => c.ReplaceItemAsync(It.IsAny<Person>(), It.IsAny<string>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()),
                             Times.Never);
        }

        [Theory]
        [ClassData(typeof(PersonLocationChangedValues))]
        public async Task FunctionQueuesChangedLocationIfLocationChanged(string existingLocation, string newLocation)
        {
            // Given
            m_LinkedPerson.Location = existingLocation;
            m_QueueItem.CorePerson.Location = newLocation;
            var function = CreateFunction();

            // When
            await function.Run(QueueMessage(), Mock.Of<ILogger>());

            // Then
            var queuedItem = m_FakeStorageQueue.GetQueuedItem<PersonLocationChangedQueueItem>(QueueStorage.QueueNames.PersonLocationChangedQueue);
            Assert.Equal(m_LinkedPerson.Id, queuedItem.PersonId);
            Assert.Equal(m_SearchFirmId, queuedItem.SearchFirmId);
        }
        
        [Theory]
        [ClassData(typeof(PersonLocationNotChangedValues))]
        public async Task FunctionDoesNotQueueChangedLocationIfLocationHasSameValue(string existingLocation, string newLocation)
        {
            // Given
            m_LinkedPerson.Location = existingLocation;
            m_QueueItem.CorePerson.Location = newLocation;
            var function = CreateFunction();

            // When
            await function.Run(QueueMessage(), Mock.Of<ILogger>());

            // Then
            Assert.Equal(0, m_FakeStorageQueue.GetQueuedItemCount(QueueStorage.QueueNames.PersonLocationChangedQueue));
        }
        
        #region Private Helpers

        private DataPoolCorePersonUpdatedFunction CreateFunction()
        {
            return new FunctionBuilder<DataPoolCorePersonUpdatedFunction>()
                  .SetFakeCosmos(m_FakeCosmos)
                  .SetFakeCloudQueue(m_FakeStorageQueue)
                  .Build();
        }

        #endregion
    }
}