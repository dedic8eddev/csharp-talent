using System.Collections.Generic;

namespace Ikiru.Parsnips.Application.Shared.Models
{
    public class Price
    {
        public int Total { get; set; }
        public int Amount { get; set; }
        public int Discount { get; set; }
        public int TaxAmount { get; set; }
        public List<string> InvalidCoupons { get; set; }
        public int UnitQuantity { get; set; }
    }
}
