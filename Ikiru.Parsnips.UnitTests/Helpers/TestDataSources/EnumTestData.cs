using System;
using System.Collections.Generic;
using System.Linq;

namespace Ikiru.Parsnips.UnitTests.Helpers.TestDataSources
{
    public class EnumTestData<TEnum> : BaseTestDataSource
    {
        private static IEnumerable<TEnum> Values => (TEnum[])Enum.GetValues(typeof(TEnum));
        public static IEnumerable<object[]> Excluding(TEnum[] exclusions) => Values.Except(exclusions).Select(s => new object[] { s });

 

        protected override IEnumerator<object[]> GetValues()
        {
            return Values.Select(v => new object[] { v })
                         .GetEnumerator();
        }
    }
}
