namespace Ikiru.Parsnips.Application.Infrastructure.Subscription.Models
{
    public class SubscriptionEstimate
    {
        public int UnitQuantity { get; set; }
        public int Total { get; set; }
        public int Amount { get; set; }
        public int Discount { get; set; }
        public int TaxAmount { get; set; }
        public bool GeneralException { get; set; }
    }
}
