using Ikiru.Parsnips.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Persons.Converters
{
    public static class UrlToTalentisSiteTypeConverter
    {
        public static WebSiteType Convert(string url)
        {
            var webSiteMapping = new Dictionary<string, WebSiteType>()
                                 {
                                     { "linkedin.com", WebSiteType.LinkedIn },
                                     { "xing.com", WebSiteType.Xing },
                                     { "crunchbase.com", WebSiteType.Crunchbase },
                                     { "reuters.com", WebSiteType.Reuters },
                                     { "bloomberg.com", WebSiteType.Bloomberg },
                                     { "zoominfo.com", WebSiteType.ZoomInfo },
                                     { "twitter.com", WebSiteType.Twitter },
                                     { "owler.com", WebSiteType.Owler },
                                     { "companieshouse.gov.uk", WebSiteType.CompaniesHouse },
                                     { "youtube.com", WebSiteType.YouTube },
                                     { "facebook.com", WebSiteType.Facebook }
                                 };

            var uri = new Uri(url);
            var host = uri.Host;
            WebSiteType siteType = WebSiteType.Other;

            foreach (var mapping in webSiteMapping)
            {
                if (host.EndsWith(mapping.Key))
                {
                    siteType = mapping.Value;
                }
            }

            return siteType;
        }
    }
}
