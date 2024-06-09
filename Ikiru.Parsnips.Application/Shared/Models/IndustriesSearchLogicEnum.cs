using System.Text.Json.Serialization;

namespace Ikiru.Parsnips.Application.Shared.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum IndustriesSearchLogicEnum
    {
        either,
        current,
        past
    }
}
