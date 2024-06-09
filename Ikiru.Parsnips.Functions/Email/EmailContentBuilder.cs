using System;
using System.IO;
using Ikiru.Parsnips.Domain;
using Microsoft.Extensions.Options;

namespace Ikiru.Parsnips.Functions.Email
{
    public class EmailContentBuilder
    {
        private readonly EmailContentSettings m_EmailContentSettings;

        public EmailContentBuilder(IOptions<EmailContentSettings> emailContentSettings)
        {
            m_EmailContentSettings = emailContentSettings.Value;
        }

        public EmailContent SearchFirmConfirmationEmail(SearchFirmUser searchFirmUser)
        {
            var confirmUrl = $"{m_EmailContentSettings.TalentisAppBaseUrl}{m_EmailContentSettings.SearchFirmConfirmationLinkPath}?{searchFirmUser.InviteToken}%7C{searchFirmUser.SearchFirmId}";
            var subject = "Talentis – Please verify your email account";

            var htmlBody = ConfirmationEmailContent(searchFirmUser, confirmUrl);

            return new EmailContent(subject,
                                    null,
                                    htmlBody);
        }


        public EmailContent InviteUserEmail(SearchFirmUser searchFirmUser, SearchFirmUser invitedBy)
        {
            var submitButtonLinkUrl = $"{m_EmailContentSettings.TalentisAppBaseUrl}{m_EmailContentSettings.NewUserConfirmationLinkPath}{searchFirmUser.InviteToken}%7C{searchFirmUser.SearchFirmId}";

            var subject = "You have been invited to join Talentis";

            var htmlBody = InviteSearchFirmUserEmailContent(invitedBy, submitButtonLinkUrl);

            return new EmailContent(subject,
                                    null,
                                    htmlBody);
        }


        private string ConfirmationEmailContent(SearchFirmUser searchFirmUser, string confirmUrl)
        {
            var userFullName = $"{searchFirmUser.FirstName} {searchFirmUser.LastName}";

            var emailContentTitle = $"Welcome to Talentis {userFullName}";
            var emailContentBody = "Thank you for creating an account with Talentis. Before you can start using Talentis we need you to verify your email address";
            var emailSubmitButtonTitle = "Confirm email account";
            return GenerateEmailContent(emailContentTitle, emailContentBody, confirmUrl, emailSubmitButtonTitle);
        }

        private string InviteSearchFirmUserEmailContent(SearchFirmUser invitedBySearchFirmUser,
                                                     string submitButtonLinkUrl)
        {
            var emailContentTitle = "Join your team at Talentis!";
            var emailContentBody = $"Your colleague at {invitedBySearchFirmUser.FullName()} {invitedBySearchFirmUser.EmailAddress} has invited you to join Talentis.";
            var emailSubmitButtonTitle = "Join your team";

            return GenerateEmailContent(emailContentTitle, emailContentBody, submitButtonLinkUrl, emailSubmitButtonTitle);
        }

        private string GenerateEmailContent(string emailContentTitle, string emailContentBody, string emailSubmitButtonUrl,
                                            string emailSubmitButtonText)
        {
            var directory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            directory = Directory.GetParent(directory).ToString();
            var htmlTemplateFile = Path.Combine(directory, "Email\\EmailTemplate.html");

            var content = File.ReadAllText(htmlTemplateFile);

            var contentTitlePlaceHolder = "{emailContentTitle}";
            var contentPlaceHolder = "{emailContent}";
            var submitButtonLinkUrlPlaceHolder = "{submitEmailLink}";
            var submitButtonTextPlaceHolder = "{emailButtonText}";
            var belowSubmitButtonTextPlaceHolder = "{belowSubmitButtonText}";

            var belowSubmitButtonText = $"If the link does not work, please copy and paste this url into your browser: {emailSubmitButtonUrl}";

            content = content.Replace(contentTitlePlaceHolder, emailContentTitle);
            content = content.Replace(contentPlaceHolder, emailContentBody);
            content = content.Replace(submitButtonTextPlaceHolder, emailSubmitButtonText);
            content = content.Replace(belowSubmitButtonTextPlaceHolder, belowSubmitButtonText);

            content = content.Replace(submitButtonLinkUrlPlaceHolder, emailSubmitButtonUrl);

            return content;
        }
    }
}