using System;
using System.Text;

namespace Ikiru.Parsnips.Application.Infrastructure.DataPool.Models.GeoData
{
    public static class AddressHelpers
    {
        public static string NormaliseAddress(params string[] addressElements)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string element in addressElements)
            {
                if (String.IsNullOrWhiteSpace(element))
                {
                    continue;
                }

                sb.Append(element.TrimEnd(',', ' ', '.'));
                sb.Append(",");
            }

            // remove trailing,
            if (sb.Length > 1)
            {
                sb.Remove(sb.Length - 1, 1);
            }

            // send
            return sb.ToString();
        }
    }
}
