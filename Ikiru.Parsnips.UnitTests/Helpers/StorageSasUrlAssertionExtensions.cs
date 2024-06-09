using System;
using System.Collections.Specialized;
using System.Web;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Helpers
{
    public static class StorageSasUrlAssertionExtensions
    {
        public static SaSUrlQueryValues AssertThatSaSUrl(this string sasAccessUrl)
        {
            var queryString = HttpUtility.ParseQueryString(sasAccessUrl);
            return new SaSUrlQueryValues(queryString);
        }

        public static SaSUrlQueryValues AssertThatSaSUrl(this Uri sasAccessUrl)
        {
            return AssertThatSaSUrl(sasAccessUrl.ToString());
        }

        public class SaSUrlQueryValues
        {
            private readonly NameValueCollection m_NameValueCollection;

            public DateTimeOffset Start => DateTimeOffset.Parse(m_NameValueCollection.Get("st")); // Start / ValidFrom
            public DateTimeOffset End => DateTimeOffset.Parse(m_NameValueCollection.Get("se")); // End / ValidUntil

            public string Signature => m_NameValueCollection.Get("sig");
            public string Permission => m_NameValueCollection.Get("sp");
            public string ContentDisposition => m_NameValueCollection.Get("rscd");
            public string ContentType => m_NameValueCollection.Get("rsct");

            public SaSUrlQueryValues(NameValueCollection nameValueCollection)
            {
                m_NameValueCollection = nameValueCollection;
            }

            public SaSUrlQueryValues HasStartNoOlderThanSeconds(int seconds)
            {
                var startDiff = DateTimeOffset.UtcNow - Start;
                Assert.True(startDiff > TimeSpan.Zero, $"Expected st to be in the past [{startDiff}]");
                Assert.True(startDiff < TimeSpan.FromSeconds(seconds), $"Expected st to be no more than {seconds} seconds in the past [{startDiff}]");
                return this;
            }

            public SaSUrlQueryValues HasEndNoMoreThanMinutesInFuture(int minutes)
            {
                var endDiff = End - DateTimeOffset.UtcNow;
                Assert.True(endDiff > TimeSpan.Zero, $"Expected se to be in the future [{endDiff}]");
                Assert.True(endDiff <= TimeSpan.FromMinutes(minutes), $"Expected se be now more that {minutes} minutes in the future [{endDiff}]");
                return this;
            }

            public SaSUrlQueryValues HasSignature()
            {
                var sig = Signature;
                Assert.NotNull(sig);
                Assert.False(string.IsNullOrWhiteSpace(sig));
                return this;
            }

            public SaSUrlQueryValues HasPermissionEquals(string expectedPermission)
            {
                Assert.Equal(expectedPermission, Permission);
                return this;
            }

            public SaSUrlQueryValues HasContentDispositionEquals(string expectedContentDisposition)
            {
                Assert.Equal(expectedContentDisposition, ContentDisposition);
                return this;
            }

            public SaSUrlQueryValues HasContentTypeEquals(string expectedContentType)
            {
                Assert.Equal(expectedContentType, ContentType);
                return this;
            }
        }
    }
}