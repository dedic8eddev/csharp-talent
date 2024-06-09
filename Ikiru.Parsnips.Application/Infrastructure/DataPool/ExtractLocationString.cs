using Ikiru.Parsnips.Application.Infrastructure.DataPool.Models.Common;
using System.Linq;

namespace Ikiru.Parsnips.Application.Infrastructure.DataPool.Models
{
    public static class ExtractLocationString
    {
        public static string FromDataPoolLocation(Address address)
        {
            //For latest version of Data
            string combinedAddress = string.Empty;
            string combinedMunicipality = string.Empty;

            if (address.CountrySecondarySubdivision?.Trim() == address.Municipality?.Trim())
            {
                combinedMunicipality = address.Municipality;
            }
            else
            {
                combinedMunicipality = string.Join(", ", (new[] {
                address.Municipality,
                address.CountrySecondarySubdivision
                })
                   .Where(y => y?.Trim().Length > 0))
                   .Trim();
            }
            combinedAddress = string.Join(", ", (new[] {
                combinedMunicipality,
                address.CountrySubdivisionName,
                address.Country
            })
                   .Where(y => y?.Trim().Length > 0))
                   .Trim();

            if (string.IsNullOrEmpty(combinedAddress))
            {
                combinedAddress = address.OriginalAddress;
            }

            //For legacy data
            if (string.IsNullOrEmpty(combinedAddress))
            {
                string.Join(", ", (new[] {
                address.CityName,
                address.CountryName
                 })
                   .Where(y => y?.Trim().Length > 0))
                   .Trim();
            }

            if (string.IsNullOrEmpty(combinedAddress))
            {
                combinedAddress = address.AddressLine;
            }

            return combinedAddress;
        }
    }
}
