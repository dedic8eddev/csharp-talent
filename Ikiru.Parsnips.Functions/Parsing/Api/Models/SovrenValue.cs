namespace Ikiru.Parsnips.Functions.Parsing.Api.Models
{
    public class SovrenValue
    {
        public string FileType { get; set; }
        public string FileExtension { get; set; }
        public string Text { get; set; }
        public DocResultCode TextCode { get; set; }
        public decimal CreditsRemaining { get; set; }
        public string ParsedDocument { get; set; }
    }
}