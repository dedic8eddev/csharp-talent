using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.Domain.Extensions;
using Ikiru.Parsnips.DomainInfrastructure;
using Ikiru.Parsnips.Functions.Email;
using Ikiru.Parsnips.Functions.Smtp;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues.Items;
using Microsoft.Azure.Storage.Queue;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Functions.Functions.SearchFirmConfirmationEmail
{
    public class ConfirmationEmailFunction
    {
        private readonly DataStore m_DataStore;
        private readonly SmtpSender m_SmtpSender;
        private readonly EmailContentBuilder m_EmailContentBuilder;

        public ConfirmationEmailFunction(DataStore dataStore, SmtpSender smtpSender, EmailContentBuilder emailContentBuilder)
        {
            m_DataStore = dataStore;
            m_SmtpSender = smtpSender;
            m_EmailContentBuilder = emailContentBuilder;
        }

        [FunctionName(nameof(ConfirmationEmailFunction))]
        public async Task Run([QueueTrigger(QueueStorage.QueueNames.SearchFirmConfirmationEmailQueue)] CloudQueueMessage queueMessage, ILogger log)
        {
            if (queueMessage.DequeueCount > 1)
                log.LogWarning($"Message {queueMessage.Id} has been dequeued multiple times. This is dequeue '{queueMessage}'");

            var queueItem = JsonSerializer.Deserialize<ConfirmationEmailQueueItem>(queueMessage.AsString);


            var searchFirmUser = await m_DataStore.Fetch<SearchFirmUser>(queueItem.SearchFirmUserId, queueItem.SearchFirmId);

            if (ShouldSendConfirmation(searchFirmUser, queueItem.ResendConfirmationEmail, log))
                await SendConfirmationEmail(searchFirmUser);

        }

        private bool ShouldSendConfirmation(SearchFirmUser searchFirmUser, bool resend, ILogger log)
        {
            var (shouldSend, notSendingReason) = searchFirmUser.ShouldSendConfirmationEmail(resend);

            if (!shouldSend)
                log.LogWarning($"{notSendingReason}. For user '{searchFirmUser.Id}'");

            return shouldSend;
        }

        private async Task SendConfirmationEmail(SearchFirmUser searchFirmUser)
        {
            EmailContent emailContent;

            if (searchFirmUser.Status == SearchFirmUserStatus.InvitedForNewSearchFirm)
            {
                emailContent = m_EmailContentBuilder.SearchFirmConfirmationEmail(searchFirmUser);
            }
            else
            {
                var invitedBy = await m_DataStore.Fetch<SearchFirmUser>(searchFirmUser.InvitedBy.Value, searchFirmUser.SearchFirmId);
                emailContent = m_EmailContentBuilder.InviteUserEmail(searchFirmUser: searchFirmUser, invitedBy: invitedBy);
            }

            await m_SmtpSender.SendEmail(emailContent, searchFirmUser.FullName(), searchFirmUser.EmailAddress, searchFirmUser.Id.ToString(),
                                            () => UpdateUserToSent(searchFirmUser));


        }

        private async Task<SearchFirmUser> UpdateUserToSent(SearchFirmUser searchFirmUser)
        {
            searchFirmUser.MarkConfirmationEmailSent();
            return await m_DataStore.Update(searchFirmUser);
        }
    }
}
