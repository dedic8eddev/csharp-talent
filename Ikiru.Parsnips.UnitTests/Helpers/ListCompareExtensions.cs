using System;
using System.Collections.Generic;
using System.Linq;
using Ikiru.Parsnips.Domain;

namespace Ikiru.Parsnips.UnitTests.Helpers
{
    public static class ListCompareExtensions
    {
        /// <summary>
        /// Determines if the lists have the same number of items in them or are both null.
        /// </summary>
        private static bool AreDifferentSize<T, T2>(this List<T> source, List<T2> compare)
        {
            // Either both null or both have same 

            if (source == null && compare == null)
            {
                return false;
            }

            if (source == null && !compare.Any())
            {
                return false;
            }

            return source?.Count != compare?.Count;
        }

        /// <summary>
        /// Determines if the lists are the same size (or both null) and if they both contain the same items
        /// </summary>
        public static bool IsSameList(this List<string> source, List<string> compare)
        {
            return !AreDifferentSize(source, compare) ||
                   source.All(compare.Contains) &&
                   compare.All(source.Contains);
        }

        // TODO: Would need to have this for other Domain Objects.
        // Could just use something like: 
        // https://github.com/jamesfoster/DeepEqual
        // https://github.com/StevenGilligan/AutoCompare
        // https://github.com/GregFinzer/Compare-Net-Objects
        public static bool IsSameList(this List<PersonDocument> source, List<PersonDocument> compare)
        {
            return !AreDifferentSize(source, compare) &&
                   source.All(s => compare.Any(c => s.Id == c.Id &&
                                                    s.SearchFirmId == c.SearchFirmId &&
                                                    s.CreatedDate == c.CreatedDate &&
                                                    s.FileName == c.FileName)) &&
                   compare.All(s => source.Any(c => s.Id == c.Id &&
                                                    s.SearchFirmId == c.SearchFirmId &&
                                                    s.CreatedDate == c.CreatedDate &&
                                                    s.FileName == c.FileName));
        }

        public static bool IsSameList<T, T2>(this List<T> source, List<T2> compare, Func<T, T2, bool> compareFunc)
        {
            return !AreDifferentSize(source, compare) &&
                   source.All(s => compare.Any(c => compareFunc(s, c))) &&
                   compare.All(s => source.Any(c => compareFunc(c, s)));
        }

        public static bool SourceItemsExistInTarget<T, T2>(this List<T> source, List<T2> compare, Func<T, T2, bool> compareFunc)
        {
            return compare.All(c => source.Any(s => compareFunc(s, c)));
        }
    }
}