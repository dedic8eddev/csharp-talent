using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Ikiru.Parsnips.Api.Recaptcha.Models
{
    public class RecaptchaVerifyResponseModel
    {   
        [JsonProperty("success")]
        public bool Success { get; set; }
        
        [JsonProperty("challenge_ts")]
        public DateTimeOffset ChallengeTimestamp { get; set; }
        
        [JsonProperty("hostname")]
        public string Hostname { get; set; }

        [JsonProperty("error-codes")]
        public IEnumerable<string> ErrorCodes { get; set; }
        
        [JsonProperty("score")]
        public double Score { get; set; }

        [JsonProperty("action")]
        public string Action { get; set; }
    }
}
