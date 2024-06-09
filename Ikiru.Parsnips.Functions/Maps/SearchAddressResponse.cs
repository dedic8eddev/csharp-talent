namespace Ikiru.Parsnips.Functions.Maps
{
    public class SearchAddressResponse
    {
        // ReSharper disable InconsistentNaming
        public Result[] results { get; set; }
        
        public class Result
        {
            public Position position { get; set; }
            public string entityType { get; set; }
            public Address address { get; set; }
        }

        public class Position
        {
            public double lat { get; set; }
            public double lon { get; set; }
        }

        public class Address
        {
            public string municipalitySubdivision { get; set; }
            public string municipality { get; set; }
            public string countrySecondarySubdivision { get; set; }
            public string countrySubdivisionName { get; set; }
            public string country { get; set; }
        }
        // ReSharper restore InconsistentNaming
    }
}