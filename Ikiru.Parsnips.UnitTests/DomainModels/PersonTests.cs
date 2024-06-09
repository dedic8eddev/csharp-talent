using Ikiru.Parsnips.Domain;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.DomainModels
{
    public class PersonTests
    {
        [Fact]
        public void CreateValidPerson()
        {
            // arrange
            var searchFirmId = Guid.NewGuid();
            var name = "John Smith";
            var location = new String('a', 100);
            var jobTitle = new String('a', 100);
            var email = "a@A.com";
            var phoneNumbers = new List<string>() { "012345678910" };
            var company = "test company 1";
            var linkedInProfileUrl = "https://linkedin.com/in/smith123";
            var keyword = "akeyword";
            var sectorId = new String('a', 7);

            var person = new Ikiru.Parsnips.Domain.Person(searchFirmId, null, linkedInProfileUrl)
            {
                Name = name,
                JobTitle = jobTitle,
                Location = location,
                Organisation = company
            };

            person.AddKeyword(keyword);
            person.AddPhoneNumbers(phoneNumbers);
            person.AddSectors(sectorId);
            person.AddTaggedEmail(email, "true");

            // act
            person.Validate();

            // assert
            Assert.False(person.ValidationResults.Any());
            Assert.Equal(name, person.Name);
            Assert.Equal(jobTitle, person.JobTitle);
            Assert.Equal(location, person.Location);
            Assert.True(person.TaggedEmails.Exists(x => x.Email == email));
            Assert.Equal(phoneNumbers, person.PhoneNumbers);
            Assert.Equal(company, person.Organisation);
            Assert.Equal(linkedInProfileUrl, person.LinkedInProfileUrl);
        }

        [Fact]
        public void CreatePersonSetsAllCollectionsToEmpty()
        {
            // arrange

            // act
            var person = new Person(Guid.NewGuid());

            // assert
            var properties = typeof(Person)
                            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                            .Where(p => p.PropertyType.GetInterfaces().Select(i => i.Name).Contains(nameof(IEnumerable))
                                   && p.PropertyType.Name != typeof(string).Name
                                   && p.Name != nameof(Person.ValidationResults));

            foreach (var propertyInfo in properties)
            {
                var value = (IEnumerable<object>)propertyInfo.GetValue(person);
                Assert.True(value != null, $"{propertyInfo.Name} is null.");
                Assert.False(value.Any(), $"{propertyInfo.Name} is not empty.");
            }
        }

        [Fact]
        public void CreateInValidPerson()
        {
            // arrange
            var keyword = new String('a', 51);
            var searchFirmId = Guid.NewGuid();
            var name = new String('a', 111);
            var jobTitle = new String('a', 122);
            var location = new String('a', 256);
            var email = new String('a', 256);
            var phoneNumbers = new List<string>() { new String('a', 28) };
            var company = new String('a', 112);
            var linkedInProfileUrl = new String('a', 150);
            string sectorId = new String('a', 8); ;

            var person = new Ikiru.Parsnips.Domain.Person(searchFirmId, null, linkedInProfileUrl)
            {
                Name = name,
                JobTitle = jobTitle,
                Location = location,
                Organisation = company
            };

            person.AddKeyword(keyword);
            person.AddPhoneNumbers(phoneNumbers);
            person.AddSectors(sectorId);
            person.AddTaggedEmail(email, "true");
            person.Validate();

            // assert
            Assert.True(person.ValidationResults.Any());
            Assert.True(person.ValidationResults.Exists(a => a.MemberNames.Contains(nameof(Person.Name))));
            Assert.True(person.ValidationResults.Exists(a => a.MemberNames.Contains(nameof(Person.JobTitle))));
            Assert.True(person.ValidationResults.Exists(a => a.MemberNames.Contains(nameof(Person.Location))));
            Assert.True(person.ValidationResults.Exists(a => a.MemberNames.Contains(nameof(Person.Organisation))));
            Assert.True(person.ValidationResults.Exists(a => a.MemberNames.Contains(nameof(Person.LinkedInProfileUrl))));
            Assert.True(person.ValidationResults.Exists(a => a.MemberNames.Contains(nameof(Person.PhoneNumbers))));
            Assert.True(person.ValidationResults.Exists(a => a.MemberNames.Contains(nameof(TaggedEmail.Email))));
            Assert.True(person.ValidationResults.Exists(a => a.MemberNames.Contains(nameof(Person.Keywords))));
            Assert.True(person.ValidationResults.Exists(a => a.MemberNames.Contains(nameof(Person.SectorsIds))));
        }

        [Fact]
        public void AddKeywordWithSectorId()
        {
            // arrange
            var searchFirmId = Guid.NewGuid();
            var linkedInProfileUrl = "https://linkedin.com/in/smith123";
            var keyword = "abc";
            var sectorId = "abc";

            var person = new Ikiru.Parsnips.Domain.Person(searchFirmId, null, linkedInProfileUrl);

             person.AddSectors(sectorId);
            person.AddKeyword(keyword);

            // act
            person.Validate();

            // assert
            Assert.False(person.ValidationResults.Exists(a => a.MemberNames.Contains(nameof(Person.SectorsIds))));
        }

        [Fact]
        public void AddKeywordWithoutSectorIdCauseError()
        {
            // arrange
            var searchFirmId = Guid.NewGuid();
            var linkedInProfileUrl = "https://linkedin.com/in/smith123";
            var keyword = "abc";

            var person = new Ikiru.Parsnips.Domain.Person(searchFirmId, null, linkedInProfileUrl);

            person.AddKeyword(keyword);

            // act
            person.Validate();

            // assert
            Assert.True(person.ValidationResults.Exists(a => a.MemberNames.Contains(nameof(Person.SectorsIds))));
        }

    }
}
