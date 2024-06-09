using Newtonsoft.Json;

namespace Ikiru.Parsnips.Domain.Chargebee
{
    public class PaymentMethod
    {
        [JsonProperty("object")]
        public string Object { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("reference_id")]
        public string ReferenceId { get; set; }

        [JsonProperty("gateway")]
        public string Gateway { get; set; }

        [JsonProperty("gateway_account_id")]
        public string GatewayAccountId { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }
    }
}
