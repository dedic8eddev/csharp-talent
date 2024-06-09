using System.Text.Json.Serialization;

namespace Ikiru.Parsnips.Application.Infrastructure.DataPool.Models.Person
{
    public class PersonDetails
    {
        public string Name { get; set; } = "";

        public string PhotoUrl { get; set; } = "";

        public string Biography { get; set; } = "";
    }
}
