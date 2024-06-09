namespace Ikiru.Parsnips.Functions.Parsing.Api
{
    public class SovrenParsedDocument
    {
        public Resume Resume { get; set; }
    }

    public class Resume
    {
        // ReSharper disable once InconsistentNaming - Classes generated from Json
        public Structuredxmlresume StructuredXMLResume { get; set; }
    }

    public class Structuredxmlresume
    {
        public Contactinfo ContactInfo { get; set; }
        public Employmenthistory EmploymentHistory { get; set; }
    }

    public class Contactinfo
    {
        public Personname PersonName { get; set; }
        public Contactmethod[] ContactMethod { get; set; }
    }

    public class Personname
    {
        public string FormattedName { get; set; }
    }

    public class Contactmethod
    {
        public string Use { get; set; }
        public string InternetEmailAddress { get; set; }
        public string InternetWebAddress { get; set; }
        public Postaladdress PostalAddress { get; set; }
        public PhoneNumber Telephone { get; set; }
        public PhoneNumber Mobile { get; set; }
    }
    
    public class Postaladdress
    {
        public string CountryCode { get; set; }
        public string[] Region { get; set; }
        public string Municipality { get; set; }
    }

    public class PhoneNumber
    {
        public string FormattedNumber { get; set; }
    }

    public class Employmenthistory
    {
        public Employerorg[] EmployerOrg { get; set; }
    }

    public class Employerorg
    {
        public string EmployerOrgName { get; set; }
        public Positionhistory[] PositionHistory { get; set; }
    }

    public class Positionhistory
    {
        public string Title { get; set; }
    }
}
