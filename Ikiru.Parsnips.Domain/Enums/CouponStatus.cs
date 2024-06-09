using System.Runtime.Serialization;

namespace Ikiru.Parsnips.Domain.Enums
{
    public enum CouponStatus
    {
        Unknown,

        [EnumMember(Value = "active")]
        Active,
        [EnumMember(Value = "expired")]
        Expired,
        [EnumMember(Value = "archived")]
        Archived,
        [EnumMember(Value = "deleted")]
        Deleted,
    }
}
