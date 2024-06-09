using System;
using System.Collections.Generic;
using System.Text;

namespace Ikiru.Parsnips.Application.Shared.Models
{
    public class Plan
    {
        public string Id { get; set; }
        public string CurrencyCode { get; set; }
        public Price Price { get; set; }
        public int Period { get; set; }
        public string PeriodUnit { get; set; }
        public string PlanType { get; set; }
        public int DefaultTokens { get; set; }
        public bool CanPurchaseRocketReach { get; set; }
    }
}
