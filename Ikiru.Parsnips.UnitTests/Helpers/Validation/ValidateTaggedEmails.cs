using System;
using Ikiru.Parsnips.Domain;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Helpers.Validation
{
    public static class ValidateTaggedEmails
    {
        /// <summary>
        /// Validate all items from expected collection are present in result collection irrespective of order
        /// </summary>
        /// <param name="expectedEmails">Collection to compare to</param>
        /// <param name="emailsResult">Collection that needs to be validated.</param>
        public static bool AssertSameList(this List<TaggedEmail> expectedEmails, IEnumerable<dynamic> emailsResult)
        {
            if (expectedEmails == null && emailsResult == null || emailsResult == null && !expectedEmails.Any())
                return true;

            if (expectedEmails == null && emailsResult == null || expectedEmails == null && !emailsResult.Any())
                return true;

            var emailsToValidate = emailsResult.ToList();
            Assert.Equal(expectedEmails.Count, emailsToValidate.Count);
            Assert.All(emailsToValidate, e => Assert.Contains(expectedEmails, te => te.SmtpValid == e.SmtpValid && te.Email == e.Email));

            return true;
        }

    }
}
