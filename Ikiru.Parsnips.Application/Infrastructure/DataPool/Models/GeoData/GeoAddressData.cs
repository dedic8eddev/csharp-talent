using System.Xml.Serialization;

namespace Ikiru.Parsnips.Application.Infrastructure.DataPool.Models.GeoData
{
    public class GeoAddressData
    {
        private bool _coordinateSet;

        /// <summary>
        /// Original address line / hint
        /// </summary>
        public string AddressLine { get; set; }

        private double _latitude;
        [XmlElement("latt")]
        public double Latitude
        {
            get => _latitude;
            set
            {
                _coordinateSet = true;
                _latitude = value;
            }
        }

        private double _longitude;

        [XmlElement("longt")]
        public double Longitude
        {
            get => _longitude;
            set
            {
                _coordinateSet = true;
                _longitude = value;
            }
        }

        [XmlElement("standard")] public GeoStandardData StandardData { get; set; }

        public class GeoStandardData
        {
            [XmlElement("stnumber")] public string StreetNumber { get; set; }
            [XmlElement("addresst")] public string Address { get; set; }
            [XmlElement("region")] public string Region { get; set; }
            [XmlElement("city")] public string City { get; set; }
            [XmlElement("postal")] public string PostalCode { get; set; }
            [XmlElement("prov")] public string CountryCode { get; set; }
            [XmlElement("countryname")] public string CountryName { get; set; }
        }

        public bool hasData => StandardData != null && _coordinateSet;
    }
}
