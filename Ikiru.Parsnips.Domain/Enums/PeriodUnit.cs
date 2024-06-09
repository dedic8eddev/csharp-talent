using System.Runtime.Serialization;

namespace Ikiru.Parsnips.Domain.Enums
{
    public enum PeriodUnitEnum
    {
        UnKnown,
        [EnumMember(Value = "day")]
        Day,
        [EnumMember(Value = "week")]
        Week,
        [EnumMember(Value = "month")]
        Month,
        [EnumMember(Value = "year")]
        Year,
        [EnumMember(Value = "not_applicable")]
        NotApplicable,
    }
}
