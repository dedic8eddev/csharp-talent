using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues.Items;
using Ikiru.Parsnips.UnitTests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ikiru.Parsnips.Functions.Functions.DataPoolPersonUpdatedWebhook;
using Ikiru.Parsnips.Functions.Functions.DataPoolPersonUpdatedWebhook.Validation;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Functions.Functions.DataPoolPersonUpdatedWebhook
{
    public class DataPoolPersonUpdatedWebhookFunctionTests
    {
        private readonly DataPoolPersonUpdatedWebhookFunction.DataPoolCorePerson m_MessageBody = new DataPoolPersonUpdatedWebhookFunction.DataPoolCorePerson
                                                                                          {
                                                                                              Id = Guid.NewGuid(),
                                                                                              FirstName = "Datary",
                                                                                              LastName = "McDataPool",
                                                                                              JobTitle = "Pool Data",
                                                                                              Company = "Data Pool PLC",
                                                                                              Location = "Datapoolville",
                                                                                              LinkedInProfileId = "data-pool-person",
                                                                                              LinkedInProfileUrl = "https://www.linkedin.com/in/data-pool-person"
                                                                                          };

        private readonly List<SearchFirm> m_SearchFirms = new List<SearchFirm>
                                                          {
                                                              new SearchFirm(),
                                                              new SearchFirm(),
                                                              new SearchFirm()
                                                          };

        private readonly FakeCosmos m_FakeCosmos = new FakeCosmos();
        private readonly FakeStorageQueue m_StorageQueue = new FakeStorageQueue();
        
        public DataPoolPersonUpdatedWebhookFunctionTests()
        {
            m_FakeCosmos.EnableContainerLinqQuery<SearchFirm, Guid>(FakeCosmos.SearchFirmsContainerName, null, () => m_SearchFirms);
        }

        [Fact]
        public async Task FunctionQueuesMessageForEachSearchFirm()
        {
            // Given
            var httpRequest = await m_MessageBody.StreamAsJsonInHttpRequest();
            var function = CreateFunction();

            // When
            await function.Run(httpRequest, Mock.Of<ILogger>());

            // Then
            var queuedItemOne = m_StorageQueue.GetQueuedItem<DataPoolCorePersonUpdatedQueueItem>(QueueStorage.QueueNames.DataPoolCorePersonUpdatedQueue);
            var queuedItemTwo = m_StorageQueue.GetQueuedItem<DataPoolCorePersonUpdatedQueueItem>(QueueStorage.QueueNames.DataPoolCorePersonUpdatedQueue);
            var queuedItemThree = m_StorageQueue.GetQueuedItem<DataPoolCorePersonUpdatedQueueItem>(QueueStorage.QueueNames.DataPoolCorePersonUpdatedQueue);

            var searchFirmsQueued = new[] { queuedItemOne, queuedItemTwo, queuedItemThree }.Select(i => i.SearchFirmId).ToList();
            Assert.Contains(m_SearchFirms[0].Id, searchFirmsQueued);
            Assert.Contains(m_SearchFirms[1].Id, searchFirmsQueued);
            Assert.Contains(m_SearchFirms[2].Id, searchFirmsQueued);

            AssertQueuedPersonDataCorrect(queuedItemOne);
            AssertQueuedPersonDataCorrect(queuedItemTwo);
            AssertQueuedPersonDataCorrect(queuedItemThree);

            void AssertQueuedPersonDataCorrect(DataPoolCorePersonUpdatedQueueItem queueItem)
            {
                var queuedPerson = queueItem.CorePerson;
                Assert.Equal(m_MessageBody.Id, queuedPerson.DataPoolPersonId);
                Assert.Equal(m_MessageBody.FirstName, queuedPerson.FirstName);
                Assert.Equal(m_MessageBody.LastName, queuedPerson.LastName);
                Assert.Equal(m_MessageBody.JobTitle, queuedPerson.JobTitle);
                Assert.Equal(m_MessageBody.Company, queuedPerson.Company);
                Assert.Equal(m_MessageBody.Location, queuedPerson.Location);
                Assert.Equal(m_MessageBody.LinkedInProfileId, queuedPerson.LinkedInProfileId);
                Assert.Equal(m_MessageBody.LinkedInProfileUrl, queuedPerson.LinkedInProfileUrl);
            }
        }
        
        [Fact]
        public async Task FunctionReturnsOkResult()
        {
            // Given
            var httpRequest = await m_MessageBody.StreamAsJsonInHttpRequest();
            var function = CreateFunction();
            
            // When
            var result = await function.Run(httpRequest, Mock.Of<ILogger>());

            // Then
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task FunctionReturnsBadRequestResultIfInvalid()
        {
            // Given
            m_MessageBody.Id = Guid.Empty;
            var httpRequest = await m_MessageBody.StreamAsJsonInHttpRequest();
            var function = CreateFunction();
            
            // When
            var result = await function.Run(httpRequest, Mock.Of<ILogger>());

            // Then
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var problemDetails = Assert.IsType<TempProblemDetails>(badRequestResult.Value);
            Assert.Equal($"'Id' must not be equal to '{Guid.Empty}'.", ((Dictionary<string, string[]>)problemDetails.Extensions["errors"])[nameof(DataPoolPersonUpdatedWebhookFunction.DataPoolCorePerson.Id)][0]);
        }
        
        #region Private Helpers

        private DataPoolPersonUpdatedWebhookFunction CreateFunction()
        {
            return new FunctionBuilder<DataPoolPersonUpdatedWebhookFunction>()
                  .SetFakeCosmos(m_FakeCosmos)
                  .SetFakeCloudQueue(m_StorageQueue)
                  .Build();
        }

        #endregion
    }
}
