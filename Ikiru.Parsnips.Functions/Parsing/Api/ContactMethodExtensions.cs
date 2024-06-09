namespace Ikiru.Parsnips.Functions.Parsing.Api
{
    public static class ContactMethodExtensions
    {
        public static string GetPhoneNumber(this Contactmethod contactMethod)
        {
            return contactMethod.Mobile != null
                       ? contactMethod.Mobile.FormattedNumber
                       : contactMethod.Telephone.FormattedNumber;
        }


        public static bool HasPhoneNumber(this Contactmethod contactMethod)
        {
            return !string.IsNullOrWhiteSpace(contactMethod.Mobile?.FormattedNumber) ||
                   !string.IsNullOrWhiteSpace(contactMethod.Telephone?.FormattedNumber);
        }
    }
}