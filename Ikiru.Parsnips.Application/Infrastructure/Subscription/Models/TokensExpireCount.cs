using System;

namespace Ikiru.Parsnips.Application.Infrastructure.Subscription.Models
{
    public class TokensExpireCount
    {
        public int Tokens { get; set; }
        public DateTimeOffset ExpiredAt { get; set; }
    }
}
