using Ikiru.Parsnips.Domain.Base;
using Newtonsoft.Json;

namespace Ikiru.Parsnips.Domain.Values
{
    /// <summary>
    /// Simple, Dumb class to avoid adding reference to Cosmos assemblies.  We just store the value at the moment, we don't
    /// query or compare etc.  Should we need to, add the Microsoft.Azure.Cosmos package and change DumbPoint usages to Point
    /// which is in the Microsoft.Azure.Cosmos.Spatial namespace.
    /// </summary>
    public class DumbPoint : ValueObject
    {
        [JsonProperty("type")]
        public string Type => "Point";
        
        [JsonProperty("coordinates")]
        public double[] Coordinates { get; }
        
        [JsonIgnore]
        public double Longitude => Coordinates[0];
        [JsonIgnore]
        public double Latitude => Coordinates[1];

        public DumbPoint(double lon, double lat)
        {
            Coordinates = new[] { lon, lat };
        }
    }
}
