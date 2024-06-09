using System.Text.Json.Serialization;

namespace Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi.Models.Person
{
    public class PersonDetails
    {
        public string Name { get; set; } = "";
        public string PhotoUrl { get; set; } = "";
        public string Biography { get; set; } = "";
    }
}
