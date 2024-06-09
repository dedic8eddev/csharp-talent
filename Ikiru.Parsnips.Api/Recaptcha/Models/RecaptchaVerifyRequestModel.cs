using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Ikiru.Parsnips.Api.Recaptcha.Models
{
    public class RecaptchaVerifyRequesteModel
    {
        [JsonProperty("secret")]
        public string Secret { get; set; }

        /// <summary>
        /// The response is the token that you are passing in from the front end recaptcha.
        /// </summary>
        [JsonProperty("response")]
        public string Response { get; set; }

    }
}
