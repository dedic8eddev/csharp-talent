using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.UnitTests.Helpers.TestDataSources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.DomainModels
{
    public class PersonGdprLawfulBasisStateTests
    {
        [Fact]
        public void PersonGdprDataOriginIsValid()
        {
            var personGdprLawfulBasisState = new PersonGdprLawfulBasisState();

            personGdprLawfulBasisState.GdprDataOrigin = new string('a', 50);
            personGdprLawfulBasisState.Validate();

            Assert.False(personGdprLawfulBasisState.ValidationResults.Any());
        }

        [Fact]
        public void PersonGdprDataOriginIsInValid()
        {
            var personGdprLawfulBasisState = new PersonGdprLawfulBasisState();

            personGdprLawfulBasisState.GdprDataOrigin = new string('a', 51);
            personGdprLawfulBasisState.Validate();

            Assert.True(personGdprLawfulBasisState.ValidationResults.Any());
        }


        [Theory]
        [ClassData(typeof(GdprValidCombinations))]
        public void PersonGDPRIsValid(PersonGdprLawfulBasisState personGdprLawfulBasisState)
        {
            personGdprLawfulBasisState.GdprDataOrigin = new string('a', 50);
            personGdprLawfulBasisState.Validate();

            Assert.False(personGdprLawfulBasisState.ValidationResults.Any());
        }

        [Theory]
        [ClassData(typeof(GdprInvalidCombinations))]
        public void PersonGDPRIsInValid(PersonGdprLawfulBasisState personGdprLawfulBasisState)
        {
            personGdprLawfulBasisState.GdprDataOrigin = new string('a', 50);
            personGdprLawfulBasisState.Validate();

            Assert.True(personGdprLawfulBasisState.ValidationResults.Any());
        }
    }

    public class GdprInvalidCombinations : BaseTestDataSource
    {
        protected override IEnumerator<object[]> GetValues()
        {
            yield return new object[]
                         {
                                 new PersonGdprLawfulBasisState()
                                 {
                                     GdprLawfulBasisOptionsStatus = GdprLawfulBasisOptionsStatusEnum.NotificationSent,
                                     GdprLawfulBasisOption = GdprLawfulBasisOptionEnum.None
                                 }
                         };
            yield return new object[]
                         {
                                 new PersonGdprLawfulBasisState()
                                 {
                                     GdprLawfulBasisOptionsStatus = GdprLawfulBasisOptionsStatusEnum.ConsentGiven,
                                     GdprLawfulBasisOption = GdprLawfulBasisOptionEnum.NotRequired
                                 }
                         };
            yield return new object[]
                         {
                                 new PersonGdprLawfulBasisState()
                                 {
                                     GdprLawfulBasisOptionsStatus = null,
                                     GdprLawfulBasisOption = GdprLawfulBasisOptionEnum.DigitalConsent
                                 }
                         };
            yield return new object[]
                         {
                                 new PersonGdprLawfulBasisState()
                                 {
                                     GdprLawfulBasisOptionsStatus = GdprLawfulBasisOptionsStatusEnum.NotificationSent,
                                     GdprLawfulBasisOption = GdprLawfulBasisOptionEnum.DigitalConsent
                                 }
                         };
            yield return new object[]
                         {
                                 new PersonGdprLawfulBasisState()
                                 {
                                     GdprLawfulBasisOptionsStatus = GdprLawfulBasisOptionsStatusEnum.Objected,
                                     GdprLawfulBasisOption = GdprLawfulBasisOptionEnum.DigitalConsent
                                 }
                         };
            yield return new object[]
                         {
                                 new PersonGdprLawfulBasisState()
                                 {
                                     GdprLawfulBasisOptionsStatus = null,
                                     GdprLawfulBasisOption = GdprLawfulBasisOptionEnum.EmailConsent
                                 }
                         };
            yield return new object[]
                         {
                                 new PersonGdprLawfulBasisState()
                                 {
                                     GdprLawfulBasisOptionsStatus = GdprLawfulBasisOptionsStatusEnum.NotificationSent,
                                     GdprLawfulBasisOption = GdprLawfulBasisOptionEnum.EmailConsent
                                 }
                         };
            yield return new object[]
                         {
                                 new PersonGdprLawfulBasisState()
                                 {
                                     GdprLawfulBasisOptionsStatus = GdprLawfulBasisOptionsStatusEnum.Objected,
                                     GdprLawfulBasisOption = GdprLawfulBasisOptionEnum.EmailConsent
                                 }
                         };
            yield return new object[]
                         {
                                 new PersonGdprLawfulBasisState()
                                 {
                                     GdprLawfulBasisOptionsStatus = null,
                                     GdprLawfulBasisOption = GdprLawfulBasisOptionEnum.VerbalConsent
                                 }
                         };
            yield return new object[]
                         {
                                 new PersonGdprLawfulBasisState()
                                 {
                                     GdprLawfulBasisOptionsStatus = GdprLawfulBasisOptionsStatusEnum.NotificationSent,
                                     GdprLawfulBasisOption = GdprLawfulBasisOptionEnum.VerbalConsent
                                 }
                         };
            yield return new object[]
                         {
                                 new PersonGdprLawfulBasisState()
                                 {
                                     GdprLawfulBasisOptionsStatus = GdprLawfulBasisOptionsStatusEnum.Objected,
                                     GdprLawfulBasisOption = GdprLawfulBasisOptionEnum.VerbalConsent
                                 }
                         };
            yield return new object[]
                         {
                                 new PersonGdprLawfulBasisState()
                                 {
                                     GdprLawfulBasisOptionsStatus = GdprLawfulBasisOptionsStatusEnum.ConsentGiven,
                                     GdprLawfulBasisOption = GdprLawfulBasisOptionEnum.LegitimateInterest
                                 }
                         };
            yield return new object[]
                         {
                                 new PersonGdprLawfulBasisState()
                                 {
                                     GdprLawfulBasisOptionsStatus = GdprLawfulBasisOptionsStatusEnum.ConsentRefused,
                                     GdprLawfulBasisOption = GdprLawfulBasisOptionEnum.LegitimateInterest
                                 }
                         };
            yield return new object[]
                         {
                                 new PersonGdprLawfulBasisState()
                                 {
                                     GdprLawfulBasisOptionsStatus = GdprLawfulBasisOptionsStatusEnum.ConsentRequestSent,
                                     GdprLawfulBasisOption = GdprLawfulBasisOptionEnum.LegitimateInterest
                                 }
                         };
        }
    }

    public class GdprValidCombinations : BaseTestDataSource
    {
        protected override IEnumerator<object[]> GetValues()
        {
            yield return new object[]
                       {
                               new PersonGdprLawfulBasisState()
                               {
                                   GdprLawfulBasisOptionsStatus = null,
                                   GdprLawfulBasisOption = GdprLawfulBasisOptionEnum.None
                               }
                       };
            yield return new object[]
                         {
                                 new PersonGdprLawfulBasisState()
                                 {
                                     GdprLawfulBasisOptionsStatus = null,
                                     GdprLawfulBasisOption = GdprLawfulBasisOptionEnum.NotRequired
                                 }
                         };
            yield return new object[]
                         {
                                 new PersonGdprLawfulBasisState()
                                 {
                                     GdprLawfulBasisOptionsStatus = GdprLawfulBasisOptionsStatusEnum.NotStarted,
                                     GdprLawfulBasisOption = GdprLawfulBasisOptionEnum.DigitalConsent
                                 }
                         };
            yield return new object[]
                         {
                                 new PersonGdprLawfulBasisState()
                                 {
                                     GdprLawfulBasisOptionsStatus = GdprLawfulBasisOptionsStatusEnum.ConsentGiven,
                                     GdprLawfulBasisOption = GdprLawfulBasisOptionEnum.DigitalConsent
                                 }
                         };
            yield return new object[]
                         {
                                 new PersonGdprLawfulBasisState()
                                 {
                                     GdprLawfulBasisOptionsStatus = GdprLawfulBasisOptionsStatusEnum.ConsentRefused,
                                     GdprLawfulBasisOption = GdprLawfulBasisOptionEnum.DigitalConsent
                                 }
                         };
            yield return new object[]
                         {
                                 new PersonGdprLawfulBasisState()
                                 {
                                     GdprLawfulBasisOptionsStatus = GdprLawfulBasisOptionsStatusEnum.ConsentRequestSent,
                                     GdprLawfulBasisOption = GdprLawfulBasisOptionEnum.DigitalConsent
                                 }
                         };
            yield return new object[]
                         {
                                 new PersonGdprLawfulBasisState()
                                 {
                                     GdprLawfulBasisOptionsStatus = GdprLawfulBasisOptionsStatusEnum.NotStarted,
                                     GdprLawfulBasisOption = GdprLawfulBasisOptionEnum.EmailConsent
                                 }
                         };
            yield return new object[]
                         {
                                 new PersonGdprLawfulBasisState()
                                 {
                                     GdprLawfulBasisOptionsStatus = GdprLawfulBasisOptionsStatusEnum.ConsentRequestSent,
                                     GdprLawfulBasisOption = GdprLawfulBasisOptionEnum.EmailConsent
                                 }
                         };
            yield return new object[]
                         {
                                 new PersonGdprLawfulBasisState()
                                 {
                                     GdprLawfulBasisOptionsStatus = GdprLawfulBasisOptionsStatusEnum.ConsentRefused,
                                     GdprLawfulBasisOption = GdprLawfulBasisOptionEnum.EmailConsent
                                 }
                         };
            yield return new object[]
                         {
                                 new PersonGdprLawfulBasisState()
                                 {
                                     GdprLawfulBasisOptionsStatus = GdprLawfulBasisOptionsStatusEnum.ConsentGiven,
                                     GdprLawfulBasisOption = GdprLawfulBasisOptionEnum.EmailConsent
                                 }
                         };
            yield return new object[]
                         {
                                 new PersonGdprLawfulBasisState()
                                 {
                                     GdprLawfulBasisOptionsStatus = GdprLawfulBasisOptionsStatusEnum.NotStarted,
                                     GdprLawfulBasisOption = GdprLawfulBasisOptionEnum.VerbalConsent
                                 }
                         };
            yield return new object[]
                         {
                                 new PersonGdprLawfulBasisState()
                                 {
                                     GdprLawfulBasisOptionsStatus = GdprLawfulBasisOptionsStatusEnum.ConsentGiven,
                                     GdprLawfulBasisOption = GdprLawfulBasisOptionEnum.VerbalConsent
                                 }
                         };
            yield return new object[]
                         {
                                 new PersonGdprLawfulBasisState()
                                 {
                                     GdprLawfulBasisOptionsStatus = GdprLawfulBasisOptionsStatusEnum.ConsentRefused,
                                     GdprLawfulBasisOption = GdprLawfulBasisOptionEnum.VerbalConsent
                                 }
                         };
            yield return new object[]
                         {
                                 new PersonGdprLawfulBasisState()
                                 {
                                     GdprLawfulBasisOptionsStatus = GdprLawfulBasisOptionsStatusEnum.ConsentRequestSent,
                                     GdprLawfulBasisOption = GdprLawfulBasisOptionEnum.VerbalConsent
                                 }
                         };
            yield return new object[]
                         {
                                 new PersonGdprLawfulBasisState()
                                 {
                                     GdprLawfulBasisOptionsStatus = GdprLawfulBasisOptionsStatusEnum.NotStarted,
                                     GdprLawfulBasisOption = GdprLawfulBasisOptionEnum.LegitimateInterest
                                 }
                         };
            yield return new object[]
                         {
                                 new PersonGdprLawfulBasisState()
                                 {
                                     GdprLawfulBasisOptionsStatus = GdprLawfulBasisOptionsStatusEnum.NotificationSent,
                                     GdprLawfulBasisOption = GdprLawfulBasisOptionEnum.LegitimateInterest
                                 }
                         };
            yield return new object[]
                         {
                                 new PersonGdprLawfulBasisState()
                                 {
                                     GdprLawfulBasisOptionsStatus = GdprLawfulBasisOptionsStatusEnum.Objected,
                                     GdprLawfulBasisOption = GdprLawfulBasisOptionEnum.LegitimateInterest
                                 }
                         };
        }
    }
}
