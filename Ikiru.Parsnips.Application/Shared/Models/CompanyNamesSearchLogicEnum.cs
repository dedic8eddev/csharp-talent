using System.Text.Json.Serialization;

namespace Ikiru.Parsnips.Application.Shared.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CompanyNamesSearchLogicEnum
    {
        either,
        current,
        past
    }
}
