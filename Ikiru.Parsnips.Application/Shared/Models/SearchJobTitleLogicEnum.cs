using System.Text.Json.Serialization;

namespace Ikiru.Parsnips.Application.Shared.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SearchJobTitleLogicEnum
    {
        either,
        current,
        past
    }
}
