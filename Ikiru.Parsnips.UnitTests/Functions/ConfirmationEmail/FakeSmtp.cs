using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Moq;

namespace Ikiru.Parsnips.UnitTests.Functions.Functions.SearchFirmConfirmationEmail
{
    public class FakeSmtp
    {
        // Test-execution-time values are loaded from appsettings.unittest.json
        private const string _EXPECTED_HOST = "unit-test.email-host.com";
        private const int _EXPECTED_PORT = 924;
        private const string _EXPECTED_USERNAME = "unit-test-smtp-user";
        private const string _EXPECTED_PASSWORD = "unit-test-smtp-pass";
        private const string _EXPECTED_FROM_ADDR = "unit-test-from@talentis.global";
        private const string _EXPECTED_FROM_NAME = "Unit Test From Name";

        public Mock<ISmtpClient> SmtpClient { get; }

        public FakeSmtp()
        {
            SmtpClient = new Mock<ISmtpClient>();
        }

        public FakeSmtp SetupForSending()
        {
            SmtpClient.Setup(s => s.ConnectAsync(It.Is<string>(h => h == _EXPECTED_HOST), It.Is<int>(p => p == _EXPECTED_PORT), It.Is<SecureSocketOptions>(o => o == SecureSocketOptions.StartTls), It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask);
            SmtpClient.Setup(s => s.AuthenticateAsync(It.Is<NetworkCredential>(c => c.UserName == _EXPECTED_USERNAME && c.Password == _EXPECTED_PASSWORD), It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask);
            SmtpClient.Setup(s => s.SendAsync(It.Is<MimeMessage>(m => m.From.Count == 1 && 
                                                                      m.From.Cast<MailboxAddress>().Single().Address == _EXPECTED_FROM_ADDR &&
                                                                      m.From.Cast<MailboxAddress>().Single().Name ==  _EXPECTED_FROM_NAME
                                                                ), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()))
                      .Returns(Task.CompletedTask);

            return this;
        }

        public FakeSmtp SetupDisconnectException<T>() where T : Exception, new()
        {
            SmtpClient.Setup(s => s.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                      .ThrowsAsync(new T());

            return this;
        }
    }
}