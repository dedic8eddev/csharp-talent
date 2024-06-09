using Ikiru.Parsnips.Application.Infrastructure.DataPool.Models.GeoData;
using System;
using System.Text.Json.Serialization;

namespace Ikiru.Parsnips.Application.Infrastructure.DataPool.Models.Common
{
    public class Address
    {
        public Address()
        {
            // create an id
            Id = Guid.NewGuid();
        }

        /// <summary>
        /// UUID of the address
        /// </summary>
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        #region OldSchema

        [JsonPropertyName("addressLine")]
        public string AddressLine { get; set; } = "";

        [JsonPropertyName("countryName")]
        public string CountryName { get; set; } = "";

        [JsonPropertyName("countryCode")]
        public string CountryCode { get; set; } = "";

        [JsonPropertyName("cityName")]
        public string CityName { get; set; } = "";

        [JsonPropertyName("postalCode")]
        public string PostalCode { get; set; } = "";

        #endregion

        /// <summary>
        /// OriginalAddress is the address as recieved from scraping in lowercase.
        /// </summary>
        [JsonPropertyName("normalizedOriginalAddress")]
        public string NormalizedOriginalAddress { get; set; }

        /// <summary>
        /// OriginalAddress is the address as recieved from scraping.
        /// </summary>
        [JsonPropertyName("originalAddress")]
        public string OriginalAddress { get; set; } = "";

        /// <summary>
        /// Geographic coordinates (latitude , longitude)
        /// </summary>
        [JsonPropertyName("geoLocation")]
        public EdmGeographyPoint GeoLocation { get; set; }

        [JsonPropertyName("streetName")]
        public string StreetName { get; set; } = "";

        [JsonPropertyName("municipalitySubdivision")]
        public string MunicipalitySubdivision { get; set; } = "";

        [JsonPropertyName("municipality")]
        public string Municipality { get; set; } = "";

        [JsonPropertyName("countryTertiarySubdivision")]
        public string CountryTertiarySubdivision { get; set; } = "";

        [JsonPropertyName("countrySecondarySubdivision")]
        public string CountrySecondarySubdivision { get; set; } = "";

        [JsonPropertyName("extendedPostalCode")]
        public string ExtendedPostalCode { get; set; } = "";

        [JsonPropertyName("postalName")]
        public string PostalName { get; set; } = "";

        [JsonPropertyName("countrySubdivisionName")]
        public string CountrySubdivisionName { get; set; } = "";

        [JsonPropertyName("countrySubdivision")]
        public string CountrySubdivision { get; set; } = "";

        [JsonPropertyName("countrySubdivisionCode")]
        public string CountrySubdivisionCode { get; set; } = "";

        [JsonPropertyName("country")]
        public string Country { get; set; } = "";

        [JsonPropertyName("countryCodeISO3")]
        public string CountryCodeISO3 { get; set; } = "";

        [JsonPropertyName("freeformAddress")]
        public string FreeformAddress { get; set; } = "";

        [JsonPropertyName("localName")]
        public string LocalName { get; set; } = "";

        [JsonPropertyName("state")]
        public string State { get; set; } = "";
    }
}
