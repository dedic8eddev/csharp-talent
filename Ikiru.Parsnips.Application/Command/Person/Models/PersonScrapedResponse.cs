using Ikiru.Parsnips.Application.Shared.Models;

namespace Ikiru.Parsnips.Application.Command.Models
{
    public class PersonScrapedResponse
    {
        public Person LocalPerson { get; set; }
        public Person DataPoolPerson { get; set; }
    }
}
