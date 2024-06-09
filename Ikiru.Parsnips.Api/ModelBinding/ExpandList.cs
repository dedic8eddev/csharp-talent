using System;
using System.Collections.Generic;

namespace Ikiru.Parsnips.Api.ModelBinding
{
    public class ExpandList<T> : List<T> where T : struct, Enum
    {
        public ExpandList() { }

        public ExpandList(int capacity) : base(capacity) { }

        public ExpandList(IEnumerable<T> fromList)
        {
            AddRange(fromList);
        }
    }
}