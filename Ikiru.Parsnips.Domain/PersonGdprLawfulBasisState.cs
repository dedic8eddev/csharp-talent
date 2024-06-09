using Ikiru.Parsnips.Domain.Base;
using Ikiru.Parsnips.Domain.DomainModel;
using Ikiru.Parsnips.Domain.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Ikiru.Parsnips.Domain
{
    public class PersonGdprLawfulBasisState : BaseModel, IValidatableObject
    {
        [MaxLength(50)]
        public string GdprDataOrigin { get; set; }

        public GdprLawfulBasisOptionEnum GdprLawfulBasisOption { get; set; }
        public GdprLawfulBasisOptionsStatusEnum? GdprLawfulBasisOptionsStatus { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var result = new List<ValidationResult>();

            if ((GdprLawfulBasisOption == GdprLawfulBasisOptionEnum.None ||
                    GdprLawfulBasisOption == GdprLawfulBasisOptionEnum.NotRequired) &&
                    GdprLawfulBasisOptionsStatus == null)
            {
                result.Add(ValidationResult.Success);
                return result;
            }

            if ((GdprLawfulBasisOption == GdprLawfulBasisOptionEnum.DigitalConsent ||
                 GdprLawfulBasisOption == GdprLawfulBasisOptionEnum.EmailConsent ||
                 GdprLawfulBasisOption == GdprLawfulBasisOptionEnum.VerbalConsent) &&
                (GdprLawfulBasisOptionsStatus == GdprLawfulBasisOptionsStatusEnum.ConsentGiven ||
                 GdprLawfulBasisOptionsStatus == GdprLawfulBasisOptionsStatusEnum.ConsentRefused ||
                 GdprLawfulBasisOptionsStatus == GdprLawfulBasisOptionsStatusEnum.ConsentRequestSent ||
                 GdprLawfulBasisOptionsStatus == GdprLawfulBasisOptionsStatusEnum.NotStarted))
            {
                result.Add(ValidationResult.Success);
                return result;
            }

            if (GdprLawfulBasisOption == GdprLawfulBasisOptionEnum.LegitimateInterest &&
                (GdprLawfulBasisOptionsStatus == GdprLawfulBasisOptionsStatusEnum.NotStarted ||
                 GdprLawfulBasisOptionsStatus == GdprLawfulBasisOptionsStatusEnum.NotificationSent ||
                 GdprLawfulBasisOptionsStatus == GdprLawfulBasisOptionsStatusEnum.Objected))
            {
                result.Add(ValidationResult.Success);
                return result;
            }

            result.Add(new ValidationResult("Failed Validation", new List<string>() { nameof(PersonGdprLawfulBasisState) }));
            return result;
        }
    }
}
