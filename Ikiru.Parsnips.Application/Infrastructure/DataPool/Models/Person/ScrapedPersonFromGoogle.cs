using System.ComponentModel.DataAnnotations;

namespace Ikiru.Parsnips.Application.Infrastructure.DataPool.Models.Person
{
    public class ScrapedPersonFromGoogle
    {
        public ScrapedPersonOriginatorTypeEnum ScrapeOriginatorType { get; set; }
        public string Title { get; set; }
        public string Snippet { get; set; }
        public string Metadata { get; set; }

        [Required]
        public string PersonUrl { get; set; }
    }
}
