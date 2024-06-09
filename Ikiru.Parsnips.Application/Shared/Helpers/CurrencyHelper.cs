using System;
using System.Collections.Generic;
using System.Text;

namespace Ikiru.Parsnips.Application.Shared.Helpers
{
    public static class CurrencyHelper
    {
        public static string GetCurrencyCodeFromCountryCode(string countryCode)
        {
            var countryCodeToCurrency = new Dictionary<string, string>()
            {
                //EUR
                {"AT","EUR"},{"BE","EUR"},{"CY","EUR"},{"EE","EUR"},{"FI","EUR"},{"FR","EUR"},{"GF","EUR"},{"TF","EUR"},{"DE","EUR"},{"GR","EUR"},{"GP","EUR"},{"VA","EUR"},{"IE","EUR"},{"IT","EUR"},{"LV","EUR"},{"LT","EUR"},{"LU","EUR"},{"MT","EUR"},{"MQ","EUR"},{"YT","EUR"},{"MC","EUR"},{"ME","EUR"},{"NL","EUR"},{"PT","EUR"},{"RE","EUR"},{"BL","EUR"},{"MF","EUR"},{"PM","EUR"},{"SM","EUR"},{"SK","EUR"},{"SI","EUR"},{"ES","EUR"},{"AX","EUR"},
                //GBP
                {"GG","GBP"},{"IM","GBP"},{"JE","GBP"},{"GB","GBP"},{"GI","GBP"},
                //AUD
                {"AU","AUD"},{"CX","AUD"},{"CC","AUD"},{"HM","AUD"},{"KI","AUD"},{"NR","AUD"},{"NF","AUD"},{"TV","AUD"}
            };

            if (!String.IsNullOrEmpty(countryCode) && countryCodeToCurrency.ContainsKey(countryCode.ToUpper()))
            {
                return countryCodeToCurrency[countryCode.ToUpper()];   
            }
            return "USD";
        }
    }
}
