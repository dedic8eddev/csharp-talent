using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.Functions.Functions.SearchFirmConfirmationEmail;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues.Items;
using Ikiru.Parsnips.UnitTests.Helpers;
using Ikiru.Parsnips.UnitTests.Helpers.TestDataSources;
using MailKit;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using MimeKit;
using Moq;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Xunit;
using Microsoft.Azure.Storage.Queue;

namespace Ikiru.Parsnips.UnitTests.Functions.Functions.SearchFirmConfirmationEmail
{
    public class ConfirmationEmailFunctionTests
    {
        private readonly Guid m_SearchFirmId = Guid.NewGuid();

        private readonly SearchFirmUser m_SearchFirmUser;
        private readonly SearchFirmUser m_InvitedByUser;

        private CloudQueueMessage m_QueueMessage;
        private readonly ConfirmationEmailQueueItem m_SearchFirmConfirmationEmailQueueItem;
        private readonly FakeSmtp m_FakeSmtp = new FakeSmtp();
        private readonly FakeCosmos m_FakeCosmos;

        public ConfirmationEmailFunctionTests()
        {
            m_InvitedByUser = new SearchFirmUser(m_SearchFirmId)
            {
                FirstName = "Boss",
                LastName = "LoseSoft",
                EmailAddress = "boss@lose.soft"
            };

            m_SearchFirmUser = new SearchFirmUser(m_SearchFirmId)
            {
                Status = SearchFirmUserStatus.InvitedForNewSearchFirm,
                InviteToken = Guid.NewGuid(),
                FirstName = "Keith",
                LastName = "McSearchFirm",
                EmailAddress = "new-user@email.com",
                JobTitle = "Searcher"
            };

            m_SearchFirmConfirmationEmailQueueItem = new ConfirmationEmailQueueItem { SearchFirmId = m_SearchFirmId, SearchFirmUserId = m_SearchFirmUser.Id };

            m_FakeCosmos = new FakeCosmos()
                          .EnableContainerFetch(FakeCosmos.SearchFirmsContainerName, m_SearchFirmUser.Id.ToString(), m_SearchFirmId.ToString(), () => m_SearchFirmUser)
                          .EnableContainerFetch(FakeCosmos.SearchFirmsContainerName, m_InvitedByUser.Id.ToString(), m_SearchFirmId.ToString(), () => m_InvitedByUser)
                          .EnableContainerReplace<SearchFirmUser>(FakeCosmos.SearchFirmsContainerName, m_SearchFirmUser.Id.ToString(), m_SearchFirmId.ToString());

            m_FakeSmtp.SetupForSending();
        }

        [Fact]
        public async Task FunctionSendsConfirmEmail()
        {
            // When
            await CreateFunction().Run(m_QueueMessage, Mock.Of<ILogger>());

            // Then
            var smtpClient = m_FakeSmtp.SmtpClient;
            smtpClient.VerifyAll();
            smtpClient.Verify(s => s.SendAsync(It.Is<MimeMessage>(m => m.To.Count == 1 &&
                                                                       m.To.Cast<MailboxAddress>().Single().Address == m_SearchFirmUser.EmailAddress &&
                                                                       m.To.Cast<MailboxAddress>().Single().Name == $"{m_SearchFirmUser.FirstName} {m_SearchFirmUser.LastName}" &&
                                                                       m.Subject == "Talentis – Please verify your email account" &&
                                                                       m.HtmlBody.Contains($"Welcome to Talentis {m_SearchFirmUser.FullName()}")
                                                                      ), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()));
        }

        [Fact]
        public async Task FunctionSendsInviteEmail()
        {
            // Given
            m_SearchFirmUser.InvitedBy = m_InvitedByUser.Id;
            m_SearchFirmUser.Status = SearchFirmUserStatus.Invited;

            // When
            await CreateFunction().Run(m_QueueMessage, Mock.Of<ILogger>());

            // Then
            var smtpClient = m_FakeSmtp.SmtpClient;
            smtpClient.VerifyAll();
            smtpClient.Verify(s => s.SendAsync(It.Is<MimeMessage>(m => m.To.Count == 1 &&
                                                              m.To.Cast<MailboxAddress>().Single().Address == m_SearchFirmUser.EmailAddress &&
                                                              m.To.Cast<MailboxAddress>().Single().Name == $"{m_SearchFirmUser.FullName()}" &&
                                                              m.From.Cast<MailboxAddress>().Single().Name == "Unit Test From Name" && //from appsettings
                                                              m.From.Cast<MailboxAddress>().Single().Address == "unit-test-from@talentis.global" && //from appsettings
                                                              m.Subject == "You have been invited to join Talentis" &&
                                                              m.HtmlBody.Contains($"Your colleague at {m_InvitedByUser.FullName()} {m_InvitedByUser.EmailAddress} has invited you to join Talentis.")
                                                                  ), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()));
        }

        [Fact]
        public async Task FunctionUpdatesSearchFirmUserWithSentFlag()
        {
            // When
            await CreateFunction().Run(m_QueueMessage, Mock.Of<ILogger>());

            // Then
            var container = m_FakeCosmos.SearchFirmsContainer;
            container.Verify(c => c.ReplaceItemAsync(It.Is<SearchFirmUser>(u => u.Id == m_SearchFirmUser.Id &&
                                                                                u.SearchFirmId == m_SearchFirmId &&
                                                                                u.Discriminator == "SearchFirmUser" &&
                                                                                u.CreatedDate == m_SearchFirmUser.CreatedDate &&
                                                                                u.IdentityUserId == m_SearchFirmUser.IdentityUserId &&
                                                                                u.InviteToken == m_SearchFirmUser.InviteToken &&
                                                                                u.FirstName == m_SearchFirmUser.FirstName &&
                                                                                u.LastName == m_SearchFirmUser.LastName &&
                                                                                u.JobTitle == m_SearchFirmUser.JobTitle &&
                                                                                u.EmailAddress == m_SearchFirmUser.EmailAddress &&
                                                                                u.Status == m_SearchFirmUser.Status &&
                                                                                u.ConfirmationEmailSent == true),
                                                     It.Is<string>(i => i == m_SearchFirmUser.Id.ToString()),
                                                     It.Is<PartitionKey>(p => p == new PartitionKey(m_SearchFirmId.ToString())),
                                                     It.IsAny<ItemRequestOptions>(),
                                                     It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task FunctionUpdatesSearchFirmUserEvenIfDisconnectFails()
        {
            // Given
            m_FakeSmtp.SetupDisconnectException<IOException>();

            // When
            var ex = await Record.ExceptionAsync(() => CreateFunction().Run(m_QueueMessage, Mock.Of<ILogger>()));

            // Then
            Assert.IsType<IOException>(ex);
            var container = m_FakeCosmos.SearchFirmsContainer;
            container.Verify(c => c.ReplaceItemAsync(It.Is<SearchFirmUser>(u => u.Status == m_SearchFirmUser.Status &&
                                                                                u.ConfirmationEmailSent &&
                                                                                u.EmailAddress == m_SearchFirmUser.EmailAddress &&
                                                                                u.FirstName == m_SearchFirmUser.FirstName &&
                                                                                u.LastName == m_SearchFirmUser.LastName &&
                                                                                u.JobTitle == m_SearchFirmUser.JobTitle &&
                                                                                u.InviteToken == m_SearchFirmUser.InviteToken),
                                                     It.Is<string>(i => i == m_SearchFirmUser.Id.ToString()),
                                                     It.Is<PartitionKey>(p => p == new PartitionKey(m_SearchFirmId.ToString())),
                                                     It.IsAny<ItemRequestOptions>(),
                                                     It.IsAny<CancellationToken>()));
        }

        [Theory]
        [MemberData(nameof(EnumTestData<SearchFirmUserStatus>.Excluding), new[]
                                                                      {
                                                                          SearchFirmUserStatus.InvitedForNewSearchFirm,
                                                                          SearchFirmUserStatus.Invited
                                                                      }, MemberType = typeof(EnumTestData<SearchFirmUserStatus>))]
        public async Task FunctionDoesNotSendEmailIfSearchFirmUserHasWrongStatus(SearchFirmUserStatus status)
        {
            // Given
            m_SearchFirmUser.Status = status;

            // When
            await CreateFunction().Run(m_QueueMessage, Mock.Of<ILogger>());

            // Then
            var smtpClient = m_FakeSmtp.SmtpClient;
            smtpClient.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task FunctionDoesNotSendEmailIfSearchFirmUserAlreadyMarkedSent()
        {
            // Given
            m_SearchFirmUser.MarkConfirmationEmailSent();

            // When
            await CreateFunction().Run(m_QueueMessage, Mock.Of<ILogger>());

            // Then
            var smtpClient = m_FakeSmtp.SmtpClient;
            smtpClient.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task FunctionResendsConfirmationEmailIfMarkedSentAndAskedToResend()
        {
            // Given
            m_SearchFirmUser.MarkConfirmationEmailSent();
            m_SearchFirmConfirmationEmailQueueItem.ResendConfirmationEmail = true;

            // When
            await CreateFunction().Run(m_QueueMessage, Mock.Of<ILogger>());

            // Then
            var smtpClient = m_FakeSmtp.SmtpClient;
            smtpClient.VerifyAll();
            smtpClient.Verify(s => s.SendAsync(It.Is<MimeMessage>(m => m.To.Count == 1 &&
                                                                       m.To.Cast<MailboxAddress>().Single().Address == m_SearchFirmUser.EmailAddress &&
                                                                       m.To.Cast<MailboxAddress>().Single().Name == $"{m_SearchFirmUser.FirstName} {m_SearchFirmUser.LastName}" &&
                                                                       m.Subject == "Talentis – Please verify your email account" &&
                                                                       m.HtmlBody.Contains($"Welcome to Talentis {m_SearchFirmUser.FullName()}")
                                                                    ), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()));
        }

        [Fact]
        public async Task FunctionResendsInviteEmailIfMarkedSentAndAskedToResend()
        {
            // Given
            m_SearchFirmUser.MarkConfirmationEmailSent();
            m_SearchFirmConfirmationEmailQueueItem.ResendConfirmationEmail = true;
            m_SearchFirmUser.InvitedBy = m_InvitedByUser.Id;
            m_SearchFirmUser.Status = SearchFirmUserStatus.Invited;

            // When
            await CreateFunction().Run(m_QueueMessage, Mock.Of<ILogger>());

            // Then
            var smtpClient = m_FakeSmtp.SmtpClient;
            smtpClient.VerifyAll();
            smtpClient.Verify(s => s.SendAsync(It.Is<MimeMessage>(m => m.To.Count == 1 &&
                                                                       m.To.Cast<MailboxAddress>().Single().Address == m_SearchFirmUser.EmailAddress &&
                                                                       m.To.Cast<MailboxAddress>().Single().Name == $"{m_SearchFirmUser.FirstName} {m_SearchFirmUser.LastName}" &&
                                                                       m.From.Cast<MailboxAddress>().Single().Name == "Unit Test From Name" && //from appsettings
                                                                       m.From.Cast<MailboxAddress>().Single().Address == "unit-test-from@talentis.global" && //from appsettings
                                                                       m.Subject == "You have been invited to join Talentis" &&
                                                                       m.HtmlBody.Contains($"Your colleague at {m_InvitedByUser.FullName()} {m_InvitedByUser.EmailAddress} has invited you to join Talentis.")
                                                                       ), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()));
        }

        #region Private Helpers

        private ConfirmationEmailFunction CreateFunction()
        {
            m_QueueMessage = new CloudQueueMessage(JsonSerializer.Serialize(m_SearchFirmConfirmationEmailQueueItem));

            return new FunctionBuilder<ConfirmationEmailFunction>()
                  .SetFakeCosmos(m_FakeCosmos)
                  .AddTransient(m_FakeSmtp.SmtpClient.Object)
                  .CopyEmailFolder()
                  .Build();
        }

        #endregion
    }
}