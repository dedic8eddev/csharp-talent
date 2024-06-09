using System.Runtime.Serialization;

namespace Ikiru.Parsnips.Domain.Enums
{
    public enum AddonType
    {
        Unknown,

        [EnumMember(Value = "plan_token")]
        PlanToken,
        [EnumMember(Value = "purchase_token")]
        PurchaseToken,
        [EnumMember(Value = "enable_purchase")]
        EnablePurchase
    }
}
