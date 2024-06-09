using System;
using System.Text.Json;

namespace Ikiru.Parsnips.Application.Command.Models
{
    public class PersonScrapedRequest
    {
        public JsonDocument ScrapedData { get; set; }
        public Guid SearchFirmId { get; set; }
        public Guid UserId { get; set; }
    }
}
