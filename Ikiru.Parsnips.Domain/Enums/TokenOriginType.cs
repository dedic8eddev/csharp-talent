using System.Runtime.Serialization;

namespace Ikiru.Parsnips.Domain.Enums
{
    public enum TokenOriginType
    {
        [EnumMember(Value = "unknown")]
        Unknown = 0,

        [EnumMember(Value = "trial")]
        Trial = 1, // do we need trial tokens?

        [EnumMember(Value = "plan")]
        Plan = 2,

        [EnumMember(Value = "purchase")]
        Purchase = 3
    }
}
