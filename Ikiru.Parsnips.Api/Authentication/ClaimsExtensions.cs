using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Ikiru.Parsnips.Api.Authentication
{
    public static class ClaimsExtensions
    {
        public static string GetClaimValue(this IEnumerable<Claim> claims, string claimType)
        {
            return claims.Single(c => c.Type == claimType).Value;
        }

        public static Guid GetGuidClaimValue(this IEnumerable<Claim> claims, string claimType)
        {
            var stringValue = claims.GetClaimValue(claimType);
            return Guid.Parse(stringValue);
        }

        public static Guid? TryGetGuidClaimValue(this IEnumerable<Claim> claims, string claimType)
        {
            var stringValue = claims.TryGetClaimValue(claimType);
            if (stringValue == null)
                return null;

            return Guid.Parse(stringValue);
        }

        public static string TryGetClaimValue(this IEnumerable<Claim> claims, string claimType) => claims.SingleOrDefault(c => c.Type == claimType)?.Value;
    }
}