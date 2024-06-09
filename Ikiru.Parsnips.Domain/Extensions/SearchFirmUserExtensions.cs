using Ikiru.Parsnips.Domain.Enums;

namespace Ikiru.Parsnips.Domain.Extensions
{
    public static class SearchFirmUserExtensions
    {
        public static (bool, string) ShouldSendConfirmationEmail(this SearchFirmUser user, bool resendConfirmationEmail)
        {
            if (!resendConfirmationEmail && user.ConfirmationEmailSent)
                return (false, "Confirmation Email already sent");

            if (user.Status != SearchFirmUserStatus.Invited && user.Status != SearchFirmUserStatus.InvitedForNewSearchFirm)
                return (false, $"User in wrong status '{user.Status}'");

            return (true, null);
        }
    }
}
