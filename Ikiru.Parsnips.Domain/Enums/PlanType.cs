using System.Runtime.Serialization;

namespace Ikiru.Parsnips.Domain.Enums
{
    public enum PlanType
    {
        Unknown,

        [EnumMember(Value = "trial")]
        Trial,
        [EnumMember(Value = "basic")]
        Basic,
        [EnumMember(Value = "connect")]
        Connect
    }
}
