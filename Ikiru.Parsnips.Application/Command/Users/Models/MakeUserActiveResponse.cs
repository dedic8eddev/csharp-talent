using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Ikiru.Parsnips.Application.Command.Users.Models
{
    public class MakeUserActiveResponse
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public UserActiveInActiveStatusEnum Response { get; set; }
    }
}
