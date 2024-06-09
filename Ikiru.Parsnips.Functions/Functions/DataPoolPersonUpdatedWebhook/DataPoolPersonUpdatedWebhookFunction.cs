using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.DomainInfrastructure;
using Ikiru.Parsnips.Functions.Functions.DataPoolPersonUpdatedWebhook.Validation;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues.Items;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Ikiru.Parsnips.Functions.Functions.DataPoolPersonUpdatedWebhook
{
    public class DataPoolPersonUpdatedWebhookFunction
    {
        private readonly DataQuery m_DataQuery;
        private readonly QueueStorage m_QueueStorage;
        private readonly IMapper m_Mapper;

        // NOTE: Talentis only requires a subset of fields to update so it doesn't want the entire DataPool Person record.
        public class DataPoolCorePerson
        {
            public Guid Id { get; set; }

            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string JobTitle { get; set; }
            public string Company { get; set; }
            public string Location { get; set; }

            public string LinkedInProfileUrl { get; set; }
            public string LinkedInProfileId { get; set; }
        }
        
        public class DataPoolCorePersonValidator : AbstractValidator<DataPoolCorePerson>
        {
            public DataPoolCorePersonValidator()
            {
                RuleFor(p => p.Id)
                   .NotEqual(Guid.Empty);
            }
        }

        public DataPoolPersonUpdatedWebhookFunction(DataQuery dataQuery, QueueStorage queueStorage, IMapper mapper)
        {
            m_DataQuery = dataQuery;
            m_QueueStorage = queueStorage;
            m_Mapper = mapper;
        }

        [FunctionName(nameof(DataPoolPersonUpdatedWebhookFunction))]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "put", Route = null)] HttpRequest httpRequest, ILogger log)
        {
            var requestMessage = await JsonSerializer.DeserializeAsync<DataPoolCorePerson>(httpRequest.Body);
            
            var validator = new DataPoolCorePersonValidator();
            var validationResult = await validator.ValidateAsync(requestMessage);

            if (!validationResult.IsValid)
                return new BadRequestObjectResult(validationResult.ToProblemDetails());

            try
            {
                // TODO: Cross-Partition Query - look to optimise later by keeping separate list of SearchFirm Ids?
                // Also: In debug, it was taking about 1m to queue up approx 200 of these, if the list of Search Firms grows, 
                // then may need to change this function to queue one message, and from that queue message, then trigger these messages
                // e.g. HTTP Function -> single queue msg -> Queue Function -> queue msg per SF -> Queue Function -> *actual processing*
                // Note: Functions consumption plan has 10 min limit of execution
                var feedIterator = m_DataQuery.GetFeedIteratorForDiscriminatedType<SearchFirm, Guid>(null, q => q.Select(s => s.Id), 20);

                while (feedIterator.HasMoreResults)
                {
                    foreach (var searchFirmId in await feedIterator.ReadNextAsync())
                    {
                        var queueItem = new DataPoolCorePersonUpdatedQueueItem
                                        {
                                            SearchFirmId = searchFirmId,
                                            CorePerson = m_Mapper.Map<DataPoolCorePersonUpdatedQueueItem.DataPoolCorePersonUpdated>(requestMessage)
                                        };
                        await m_QueueStorage.EnqueueAsync(QueueStorage.QueueNames.DataPoolCorePersonUpdatedQueue, queueItem);
                    }
                }
            }
            catch (Exception)
            {
                log.LogError($"Exception processing DataPool person: '{requestMessage.Id}'");
                throw;
            }
            
            return new OkResult();
        }
    }
}
