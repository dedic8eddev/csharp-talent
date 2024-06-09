using System;

namespace Ikiru.Parsnips.Application.Infrastructure.Subscription.Models
{
    public class CreatedPaymentIntent
    {
        public bool GeneralException { get; set; }
        public string ReferenceId { get; set; }
        public string Gateway { get; set; }
        public string Id { get; set; }
        public StatusEnum Status { get; set; }
        public string CurrencyCode { get; set; }
        public int Amount { get; set; }
        public string GatewayAccountId { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string CustomerId { get; set; }
        public PaymentMethodTypeEnum? PaymentMethodType { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }

        public enum StatusEnum
        {
            UnKnown = 0,
            Invited = 1,
            InProgress = 2,
            Authorized = 3,
            Consumed = 4,
            Expired = 5
        }
        public enum PaymentMethodTypeEnum
        {
            UnKnown = 0,
            Card = 1,
            Ideal = 2,
            Sofort = 3,
            Bancontact = 4,
            GooglePay = 5,
            Dotpay = 6,
            Giropay = 7
        }
    }
}
