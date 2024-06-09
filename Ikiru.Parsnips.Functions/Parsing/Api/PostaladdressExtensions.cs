using System.Collections.Generic;

namespace Ikiru.Parsnips.Functions.Parsing.Api
{
    public static class PostalAddressExtensions
    {
        public static string Flatten(this Postaladdress postalAddress)
        {
            var items = new List<string>();

            var municipality = postalAddress.Municipality.Trim();
            if (!string.IsNullOrWhiteSpace(municipality))
                items.Add(municipality);

            if (postalAddress.Region != null && postalAddress.Region.Length > 0)
            {
                var region = postalAddress.Region[0].Trim();
                if (!string.IsNullOrWhiteSpace(region))
                    items.Add(region);
            }

            var countryCode = postalAddress.CountryCode.Trim();
            if (!string.IsNullOrWhiteSpace(countryCode))
                items.Add(countryCode);

            return string.Join(", ", items);
        }
    }
}