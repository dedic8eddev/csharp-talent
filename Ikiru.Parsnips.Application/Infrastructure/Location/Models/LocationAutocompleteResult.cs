using System.Text.Json.Serialization;

namespace Ikiru.Parsnips.Application.Infrastructure.Location.Models
{
    public class Summary
    {
        [JsonPropertyName("query")]
        public string Query { get; set; }

        [JsonPropertyName("queryType")]
        public string QueryType { get; set; }

        [JsonPropertyName("queryTime")]
        public int QueryTime { get; set; }

        [JsonPropertyName("numResults")]
        public int NumResults { get; set; }

        [JsonPropertyName("offset")]
        public int Offset { get; set; }

        [JsonPropertyName("totalResults")]
        public int TotalResults { get; set; }

        [JsonPropertyName("fuzzyLevel")]
        public int FuzzyLevel { get; set; }
    }

    public class Address
    {
        [JsonPropertyName("municipalitySubdivision")]
        public string MunicipalitySubdivision { get; set; }

        [JsonPropertyName("municipality")]
        public string Municipality { get; set; }

        [JsonPropertyName("countryTertiarySubdivision")]
        public string CountryTertiarySubdivision { get; set; }

        [JsonPropertyName("countrySecondarySubdivision")]
        public string CountrySecondarySubdivision { get; set; }

        [JsonPropertyName("countrySubdivision")]
        public string CountrySubdivision { get; set; }

        [JsonPropertyName("countrySubdivisionCode")]
        public string CountrySubdivisionCode { get; set; }

        [JsonPropertyName("countrySubdivisionName")]
        public string CountrySubdivisionName { get; set; }

        [JsonPropertyName("countryCode")]
        public string CountryCode { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; }

        [JsonPropertyName("countryCodeISO3")]
        public string CountryCodeISO3 { get; set; }

        [JsonPropertyName("freeformAddress")]
        public string FreeformAddress { get; set; }
    }

    public class CoordinateAbbreviated
    {
        [JsonPropertyName("lat")]
        public double Lat { get; set; }

        [JsonPropertyName("lon")]
        public double Lon { get; set; }
    }

    public class Viewport
    {
        [JsonPropertyName("topLeftPoint")]
        public CoordinateAbbreviated TopLeftPoint { get; set; }

        [JsonPropertyName("btmRightPoint")]
        public CoordinateAbbreviated BtmRightPoint { get; set; }
    }

    public class Geometry
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
    }

    public class DataSources
    {
        [JsonPropertyName("geometry")]
        public Geometry Geometry { get; set; }
    }

    public class LocationDetails
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("score")]
        public double Score { get; set; }

        [JsonPropertyName("dist")]
        public double Dist { get; set; }

        [JsonPropertyName("entityType")]
        public string EntityType { get; set; }

        [JsonPropertyName("address")]
        public Address Address { get; set; }

        [JsonPropertyName("position")]
        public CoordinateAbbreviated Position { get; set; }

        [JsonPropertyName("viewport")]
        public Viewport Viewport { get; set; }

        [JsonPropertyName("boundingBox")]
        public Viewport BoundingBox { get; set; }

        [JsonPropertyName("dataSources")]
        public DataSources DataSources { get; set; }
    }

    public class LocationAutocompleteResult
    {
        [JsonPropertyName("summary")]
        public Summary Summary { get; set; }

        [JsonPropertyName("results")]
        public LocationDetails[] Results { get; set; }
    }
}
