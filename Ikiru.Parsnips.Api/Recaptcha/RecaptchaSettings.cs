using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Recaptcha
{
    public class RecaptchaSettings
    {
        public string BaseUrl { get; set; }      
        public string Secret { get; set; }
    }
}
