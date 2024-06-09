using Ikiru.Parsnips.Domain.Enums;

namespace Ikiru.Parsnips.Application.Shared.Models
{
    public class PersonGdprLawfulBasisState
    {
        public string GdprDataOrigin { get; set; }
        public GdprLawfulBasisOptionEnum? GdprLawfulBasisOption { get; set; }
        public GdprLawfulBasisOptionsStatusEnum? GdprLawfulBasisOptionsStatus { get; set; }
    }
}
