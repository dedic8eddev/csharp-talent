using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Functions.Email;
using Microsoft.Extensions.Options;
using Moq;
using System;
using Ikiru.Parsnips.Functions.Functions.SearchFirmConfirmationEmail;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Functions.Functions.Emails
{
    public class UserEmailTests
    {
        private Mock<IOptions<EmailContentSettings>> m_EmailContentSettingsMock;
        private SearchFirmUser invitedBySearchFirmUserStub;
        private SearchFirmUser invitedSendToSearchFirmUserStub;
        private Guid m_SearchFirmId = Guid.NewGuid();
        private EmailContentSettings m_EmailContentSettingsStub;

        public UserEmailTests()
        {
            new FunctionBuilder<ConfirmationEmailFunction>().CopyEmailFolder();
        }

        [Fact]
        public void InvitedUserEmailContentBuilder()
        {
            //  Givem
            m_EmailContentSettingsStub = new EmailContentSettings
            {
                NewUserConfirmationLinkPath = "https://ALink.com/123456",
                SearchFirmConfirmationLinkPath = "https://ALink.com/123456",
                SearchFirmConfirmationSubject = "Subject line",
                TalentisAppBaseUrl = "https://talentis.com/inviteduser/"
            };

            invitedSendToSearchFirmUserStub = new SearchFirmUser(m_SearchFirmId)
            {
                InviteToken = Guid.NewGuid()
            };

            invitedBySearchFirmUserStub = new SearchFirmUser(m_SearchFirmId)
            {
                FirstName = "John",
                LastName = "Smith",
                EmailAddress = "john@smith.com"
            };

            m_EmailContentSettingsMock = new Mock<IOptions<EmailContentSettings>>();

            m_EmailContentSettingsMock.Setup(x => x.Value).Returns(m_EmailContentSettingsStub);

            var emailContentBuilder = new EmailContentBuilder(m_EmailContentSettingsMock.Object);

            // When
            var result = emailContentBuilder.InviteUserEmail(invitedSendToSearchFirmUserStub, invitedBySearchFirmUserStub);

            // Then

            // assert contains throws exception, probably due to large string

            Assert.Contains($"{invitedBySearchFirmUserStub.FirstName} {invitedBySearchFirmUserStub.LastName}", result.HtmlBody);
            Assert.Contains(invitedBySearchFirmUserStub.EmailAddress, result.HtmlBody);
            Assert.Contains("Join your team", result.HtmlBody);
            Assert.Equal("You have been invited to join Talentis", result.Subject);
        }

        [Fact]
        public void ConfirmationEmailContentBuilder()
        {
            //  Given

            m_EmailContentSettingsStub = new EmailContentSettings
            {
                NewUserConfirmationLinkPath = "https://ALink.com/123456",
                SearchFirmConfirmationLinkPath = "https://ALink.com/123456",
                SearchFirmConfirmationSubject = "Subject line",
                TalentisAppBaseUrl = "https://talentis.com/inviteduser/"
            };

            invitedBySearchFirmUserStub = new SearchFirmUser(m_SearchFirmId)
            {
                FirstName = "John",
                LastName = "Smith",
                EmailAddress = "john@smith.com"
            };

            var userFullName = $"{invitedBySearchFirmUserStub.FirstName} {invitedBySearchFirmUserStub.LastName}";

            invitedBySearchFirmUserStub = new SearchFirmUser(m_SearchFirmId)
            {
                FirstName = "John",
                LastName = "Smith",
                EmailAddress = "john@smith.com"
            };

            m_EmailContentSettingsMock = new Mock<IOptions<EmailContentSettings>>();

            m_EmailContentSettingsMock.Setup(x => x.Value).Returns(m_EmailContentSettingsStub);

            var emailContentBuilder = new EmailContentBuilder(m_EmailContentSettingsMock.Object);

            // When
            var result = emailContentBuilder.SearchFirmConfirmationEmail(invitedBySearchFirmUserStub);

            // Then

            // assert contains throws exception, probably due to large string
            Assert.Equal("Talentis – Please verify your email account", result.Subject);
            Assert.Contains($"Welcome to Talentis {userFullName}", result.HtmlBody);
            Assert.Contains("Thank you for creating an account with Talentis. Before you can start using Talentis we need you to verify your email address", result.HtmlBody);
            Assert.Contains("Confirm email account", result.HtmlBody);

        }
    }
}
