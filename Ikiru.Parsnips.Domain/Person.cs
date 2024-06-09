using Ikiru.Parsnips.Domain.Base;
using Ikiru.Parsnips.Domain.Values;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Ikiru.Parsnips.Domain.Enums;
using System.Text.RegularExpressions;
using System.ComponentModel.DataAnnotations;
using Ikiru.Parsnips.Domain.ValidationAttributes;

namespace Ikiru.Parsnips.Domain
{
    public class Person : MultiTenantedDomainObject
    {
        static readonly Regex _profileUrl = new Regex(@"^https://(?:\w+\.)*linkedin.com/in/\w+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        [MaxLength(110)]
        public string Name { get; set; }

        [MaxLength(255)]
        public string Location { get; set; }

        [MaxLength(121)]
        public string JobTitle { get; set; }

        [MaxLength(111)]
        public string Organisation { get; set; }

        public string Bio { get; set; }

        private List<ScrapedDataForPerson> _scrapedData;

        public List<ScrapedDataForPerson> ScrapedData
        {
            get => _scrapedData ??= new List<ScrapedDataForPerson>();
            set => _scrapedData = value;
        }

        private List<TaggedEmail> _emailAddresses;
        public List<TaggedEmail> TaggedEmails
        {
            get => _emailAddresses ??= new List<TaggedEmail>();
            set => _emailAddresses = value;
        }

        private List<string> _phoneNumbers;

        [CustomValidation(typeof(Person), nameof(PhoneNumberValidator))]
        public List<string> PhoneNumbers
        {
            get => _phoneNumbers ??= new List<string>();
            set => _phoneNumbers = value;
        }

        public string ImportedLinkedInProfileUrl { get; set; }
        public string ImportedLinkedInCompanyUrl { get; set; }

        /* Immutable Properties */

        public Guid? ImportId { get; }

        [MaxLength(150)]
        [CustomValidation(typeof(Person), nameof(LinkedInUrlValidator))]
        public string LinkedInProfileUrl { get; set; }

        public string LinkedInProfileId { get; set; }

        public DumbPoint Geolocation { get; private set; }
        public string GeolocationDescription { get; private set; }

        public Guid? DataPoolPersonId { get; set; }

        /* Child Properties */

        public PersonGdprLawfulBasisState GdprLawfulBasisState { get; set; }

        private List<PersonDocument> _documents;

        public List<PersonDocument> Documents
        {
            get => _documents ??= new List<PersonDocument>();
            set => _documents = value;
        }


        private List<string> _keywords;

        [CustomValidation(typeof(Person), nameof(KeywordsValidator))]
        [GroupedPropertiesRequiredNotEmpty(new string[] { nameof(Keywords), nameof(SectorsIds) })]
        public List<string> Keywords
        {
            get => _keywords ??= new List<string>();
            set => _keywords = value;
        }

        private List<string> _sectorIds;

        [CustomValidation(typeof(Person), nameof(SectorIdsValidator))]
        public List<string> SectorsIds
        {
            get => _sectorIds ??= new List<string>();
            set => _sectorIds = value;
        }

        private List<PersonWebsite> _webSites;

        public List<PersonWebsite> WebSites
        {
            get => _webSites ??= new List<PersonWebsite>();
            set => _webSites = value;
        }

        public bool RocketReachFetchedInformation { get; set; }

        // Todo: Remove parameterless constructor
        [Obsolete("We should not use parameterless constructor here as SearchFirmId is mandatory field")]
        public Person() : base(default, default, default)
        {

        }

        /* Serialiser Constructor */
        [JsonConstructor]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Serialisation Ctor")]
        private Person(Guid id, DateTimeOffset createdDate, Guid searchFirmId, Guid? importId, string linkedInProfileUrl, string linkedInProfileId, DumbPoint geolocation, string geolocationDescription, Guid? dataPoolPersonId) : base(id, createdDate, searchFirmId)
        {
            ImportId = importId;
            LinkedInProfileUrl = linkedInProfileUrl;
            LinkedInProfileId = NormaliseLinkedInProfileUrl(linkedInProfileUrl);
            Geolocation = geolocation;
            GeolocationDescription = geolocationDescription;
            DataPoolPersonId = dataPoolPersonId;
        }

        /* Business Logic Constructor */
        public Person(Guid searchFirmId, Guid? importId = null, string linkedInProfileUrl = null) : base(searchFirmId)
        {
            ImportId = importId;
            LinkedInProfileUrl = linkedInProfileUrl;
            LinkedInProfileId = NormaliseLinkedInProfileUrl(linkedInProfileUrl);
        }

        public void ChangeLinkedInProfileUrl(string linkedInProfileUrl)
        {
            LinkedInProfileUrl = linkedInProfileUrl;
            LinkedInProfileId = NormaliseLinkedInProfileUrl(linkedInProfileUrl);
        }

        public static string SanitiseUrlOfLinkedInProfile(string profileUrl)
        {
            if (string.IsNullOrWhiteSpace(profileUrl))
                return $"Empty-{Guid.NewGuid()}"; // Unfortunately this is the infrastructure bleeding into the Domain: Cosmos unique indexes don't allow Sparse values, so we must provide a unique value.

            var uriBuilder = new UriBuilder(profileUrl.ToLower());
            var pathSegments = uriBuilder.Uri.Segments;

            // Unique part is after the "/in/"
            var rootFolderIndex = Array.IndexOf(pathSegments, "in/");
            var redirectRootFolderIndex = Array.IndexOf(pathSegments, "pub/");
            if (rootFolderIndex == -1 || (redirectRootFolderIndex != -1 && redirectRootFolderIndex < rootFolderIndex))
                rootFolderIndex = redirectRootFolderIndex;

            var profileId = rootFolderIndex == -1 // Not found
                                ? string.Empty
                                : pathSegments.Length == rootFolderIndex + 1 // Check /in/ isn't at the end
                                    ? string.Empty
                                    : pathSegments[rootFolderIndex + 1].TrimEnd('/');

            return $"https://www.linkedin.com/in/{profileId}";
        }



        public bool HasLocation() => !string.IsNullOrWhiteSpace(Location);

        public void SetGeolocation(double lon, double lat, string description)
        {
            Geolocation = new DumbPoint(lon, lat);
            GeolocationDescription = description;
        }

        public void RemoveGeolocation()
        {
            Geolocation = null;
            GeolocationDescription = null;
        }

        public void SetDataPoolPersonId(Guid dataPoolPersonId)
        {
            if (DataPoolPersonId.HasValue)
                throw new InvalidOperationException("No business logic for changing DataPool Person ID yet.");

            DataPoolPersonId = dataPoolPersonId;
        }


        // The collection of sites is not big and we do not expect profiles to have big lists, so, I decided to not optimize the sort and mapping.
        // I thought of something like Dictionary<> { "uk", Dictionary<> { "gov", Dictionary<> { "companieshouse", WebSiteType.CompaniesHouse } } }...
        private static readonly List<PersonWebsite> s_WebSiteMapping = new List<PersonWebsite>
                                                                       {
                                                                           new PersonWebsite { Url = "linkedin.com", Type = WebSiteType.LinkedIn },
                                                                           new PersonWebsite { Url = "xing.com", Type = WebSiteType.Xing },
                                                                           new PersonWebsite { Url = "crunchbase.com", Type = WebSiteType.Crunchbase },
                                                                           new PersonWebsite { Url = "reuters.com", Type = WebSiteType.Reuters },
                                                                           new PersonWebsite { Url = "bloomberg.com", Type = WebSiteType.Bloomberg },
                                                                           new PersonWebsite { Url = "zoominfo.com", Type = WebSiteType.ZoomInfo },
                                                                           new PersonWebsite { Url = "twitter.com", Type = WebSiteType.Twitter },
                                                                           new PersonWebsite { Url = "owler.com", Type = WebSiteType.Owler },
                                                                           new PersonWebsite { Url = "companieshouse.gov.uk", Type = WebSiteType.CompaniesHouse },
                                                                           new PersonWebsite { Url = "youtube.com", Type = WebSiteType.YouTube },
                                                                           new PersonWebsite { Url = "facebook.com", Type = WebSiteType.Facebook }
                                                                       };

        // This is risky - the behaviour is that GET should return websites in correct order.  Instead we are setting order before persistence.
        // I'd also have preferred we model this immutably on the Person if arbitrarily setting a value on the Domain Object (e.g. Person.WebSites.Add(...)) is
        // potentially putting it in an invalid state - for now we have a method that must be called after. e.g. have Person.AddWebsite(string webSite) or 
        // Person.SetWebSites(List<string> webSites)
        public void SetWebSiteTypesAndSort()
        {
            foreach (var webSite in WebSites)
            {
                var uri = new Uri(webSite.Url);
                var host = uri.Host;

                webSite.Type = s_WebSiteMapping.FirstOrDefault(w => host.EndsWith(w.Url))?.Type ?? WebSiteType.Other;
            }

            // Microsoft docs says IEnumerable.OrderBy performs a stable sort which means we can rely that out order is same as in order for same types
            // https://docs.microsoft.com/en-us/dotnet/api/system.linq.enumerable.orderby?view=netcore-3.1
            WebSites = WebSites.OrderBy(s => s.Type).ToList();
        }

        public void AddSectors(string sectorId)
        {
            if (SectorsIds == null)
            {
                SectorsIds = new List<string>();
            }

            SectorsIds.Add(sectorId);
        }


        public void AddTaggedEmail(string emailAddress, string smtpValid = "false")
        {
            if (TaggedEmails == null)
            {
                TaggedEmails = new List<TaggedEmail>();
            }

            var taggedEmail = new TaggedEmail()
            {
                Email = emailAddress,
                SmtpValid = smtpValid
            };

            TaggedEmails.Add(taggedEmail);

        }

        public static ValidationResult KeywordsValidator(List<string> keywords)
        {
            foreach (var keyword in keywords)
            {
                if (keyword.Length > 50)
                {
                    return new ValidationResult("Failed validation", new List<string>() { nameof(Person.Keywords) });
                }
            }

            return ValidationResult.Success;
        }


        public void AddKeyword(string keyword)
        {
            if (Keywords == null)
            {
                Keywords = new List<string>();
            }

            Keywords.Add(keyword);
        }

        public static string NormaliseLinkedInProfileUrl(string profileUrl)
        {
            if (string.IsNullOrWhiteSpace(profileUrl))
                return $"Empty-{Guid.NewGuid()}"; // Unfortunately this is the infrastructure bleeding into the Domain: Cosmos unique indexes don't allow Sparse values, so we must provide a unique value.

            var uriBuilder = new UriBuilder(profileUrl.ToLower());
            var pathSegments = uriBuilder.Uri.Segments;

            // Unique part is after the "/in/"
            var rootFolderIndex = Array.IndexOf(pathSegments, "in/");
            var redirectRootFolderIndex = Array.IndexOf(pathSegments, "pub/");
            if (rootFolderIndex == -1 || (redirectRootFolderIndex != -1 && redirectRootFolderIndex < rootFolderIndex))
                rootFolderIndex = redirectRootFolderIndex;

            var profileId = rootFolderIndex == -1 // Not found
                                ? string.Empty
                                : pathSegments.Length == rootFolderIndex + 1 // Check /in/ isn't at the end
                                    ? string.Empty
                                    : pathSegments[rootFolderIndex + 1].TrimEnd('/');

            return profileId;
        }


        public static ValidationResult SectorIdsValidator(List<string> sectorIds)
        {
            foreach (var sectorId in sectorIds)
            {
                if (string.IsNullOrWhiteSpace(sectorId) ||
                        sectorId.Length > 7)
                {
                    return new ValidationResult("Failed validation", new List<string>() { nameof(Person.SectorsIds) });
                }
            }

            return ValidationResult.Success;
        }

        public static ValidationResult LinkedInUrlValidator(string linkedInProfileUrl)
        {
            if (linkedInProfileUrl == null)
            {
                return ValidationResult.Success;
            }

            if (_profileUrl.IsMatch(linkedInProfileUrl))
            {
                return ValidationResult.Success;
            }

            return new ValidationResult("failed", new List<string> { nameof(Person.LinkedInProfileUrl) });

        }


        public static ValidationResult PhoneNumberValidator(List<string> phoneNumbers)
        {
            foreach (var number in phoneNumbers)
            {
                if (number.Length > 27)
                {
                    return new ValidationResult("failed validation", new List<string> { nameof(Person.PhoneNumbers) });
                }
            }

            return ValidationResult.Success;
        }

        public void AddPhoneNumbers(List<string> phoneNumber)
        {
            if (PhoneNumbers == null)
            {
                PhoneNumbers = new List<string>();
            }

            if (phoneNumber == null)
            {
                return;
            }

            PhoneNumbers.AddRange(phoneNumber);
        }

    }
}
