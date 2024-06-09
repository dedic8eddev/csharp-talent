using System.Text.Json.Serialization;

namespace Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi.Models.GeoData
{
        public class EdmGeographyPoint
        {
            public EdmGeographyPoint()
            {
                
            }

            public EdmGeographyPoint(double longitude, double latitude)
            {
                Coordinates = new double[2];
                Coordinates[0] = longitude;
                Coordinates[1] = latitude;
                Type = "Point";
            }

            [JsonPropertyName("type")]
            public string Type { get; set; }

            [JsonPropertyName("coordinates")]
            public double[] Coordinates { get; set; }
        }
}
