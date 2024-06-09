using System.Runtime.Serialization;

namespace Ikiru.Parsnips.Domain.Enums
{
    public enum AddonStatus
    {
        UnKnown,
        [EnumMember(Value = "active")]
        Active,
        [EnumMember(Value = "archived")]
        Archived,
        [EnumMember(Value = "deleted")]
        Deleted
    }
}
