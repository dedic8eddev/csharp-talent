using System;
using System.Net;
using System.Threading.Tasks;
using Ikiru.Parsnips.Functions.Email;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Ikiru.Parsnips.Functions.Smtp
{
    public class SmtpSender
    {
        private readonly ISmtpClient m_SmtpClient;
        private readonly SmtpSendingSettings m_Settings;
        private readonly ILogger<SmtpSender> m_Logger;

        public SmtpSender(ISmtpClient smtpClient, IOptions<SmtpSendingSettings> settings, ILogger<SmtpSender> logger)
        {
            m_SmtpClient = smtpClient;
            m_Settings = settings.Value;
            m_Logger = logger;
        }

        public async Task SendEmail(EmailContent emailContent, string toName, string toEmail, string logEntryUniqueId, Func<Task> onSent)
        {
            var body = new BodyBuilder
                       {
                           TextBody = emailContent.TextBody,
                           HtmlBody = emailContent.HtmlBody
                       };

            var message = new MimeMessage
                          {
                              From =
                              {
                                  new MailboxAddress(m_Settings.FromName, m_Settings.FromAddress)
                              },
                              To =
                              {
                                  new MailboxAddress(toName, toEmail)
                              },
                              Subject = emailContent.Subject,
                              Body = body.ToMessageBody()
                          };

            try
            {
                m_Logger.LogInformation($"Connecting to '{m_Settings.Host}:{m_Settings.Port}' for sending to '{logEntryUniqueId}'");
                await m_SmtpClient.ConnectAsync(m_Settings.Host, m_Settings.Port, SecureSocketOptions.StartTls);
                m_Logger.LogInformation($"Authenticating for sending to '{logEntryUniqueId}'");
                await m_SmtpClient.AuthenticateAsync(new NetworkCredential(m_Settings.Username, m_Settings.Password));
                m_Logger.LogInformation($"Sending confirmation email to '{logEntryUniqueId}'");
                await m_SmtpClient.SendAsync(message);
            }
            finally
            {
                try
                {
                    await m_SmtpClient.DisconnectAsync(true);
                }
                catch
                {
                    await onSent();
                    throw;
                }
            }
            
            await onSent();
        }
    }
}