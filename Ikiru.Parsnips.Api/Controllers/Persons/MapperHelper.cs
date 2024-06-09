using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Persons
{
    static public class MapperHelper
    {
        public static GetSimilarList.Result.Person.Address FullAddressToSearchAddress(Shared.Infrastructure.DataPoolApi.Models.Common.Address address)
        {
            string combinedMunicipality = string.Empty;
            var result = new GetSimilarList.Result.Person.Address()
            {
                AddressLine = address.AddressLine,
                CityName = address.CityName,
                CountryName = address.CountryName
            };

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

            if (string.IsNullOrEmpty(address.AddressLine))
            {
                result.AddressLine = combinedMunicipality;
            } 

            if (string.IsNullOrEmpty(address.CityName))
            {
                result.CityName = address.CountrySubdivision;
            }

            if (string.IsNullOrEmpty(address.CountryName))
            {
                result.CityName = address.Country;
            }

            return result;
        }
    }
}
