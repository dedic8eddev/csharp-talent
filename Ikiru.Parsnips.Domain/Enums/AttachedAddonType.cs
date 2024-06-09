using System.Runtime.Serialization;

namespace Ikiru.Parsnips.Domain.Enums
{
    public enum AttachedAddonType
    {
        UnKnown,
        [EnumMember(Value = "recommended")]
        Recommended,
        [EnumMember(Value = "mandatory")]
        Mandatory,
    }
}
