using System.Runtime.Serialization;

namespace Ikiru.Parsnips.Domain.Enums
{
    public enum PlanStatus
    {
        Unknown,

        [EnumMember(Value = "active")]
        Active,
        [EnumMember(Value = "archived")]
        Archived,
    }
}
