using Ikiru.Parsnips.Api.Controllers.Persons;
using Ikiru.Parsnips.Application.Infrastructure.DataPool;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi;
using Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi.Models.Common;
using Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi.Models.GeoData;
using Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi.Models.Person;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues.Items;
using Ikiru.Parsnips.UnitTests.Helpers;
using Ikiru.Parsnips.UnitTests.Helpers.TestDataSources;
using Ikiru.Parsnips.UnitTests.Helpers.Validation;
using Ikiru.Persistence.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Person = Ikiru.Parsnips.Domain.Person;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Persons
{
    public class PutTests
    {
        private readonly Guid m_SearchFirmId = Guid.NewGuid();
        private const string _PROFILE_ID = "gruffrhys";

        private readonly Put.Command m_Command = new Put.Command
        {
            Name = "Gruff Rhys",
            JobTitle = "Lead Singer",
            Location = "Haverfordwest, Pembrokeshire, Wales",
            TaggedEmails = new List<Put.BasePutPerson.TaggedEmail> { new Put.BasePutPerson.TaggedEmail { Email = "gruff@gruffrhys.com" }, new Put.BasePutPerson.TaggedEmail { Email = "band@superfurry.com" } },
            PhoneNumbers = new List<string> { "01234 987564", "00000 333333" },
            Company = "Super Furry Animals",
            Bio = "Won a medal, participated in events, achieved something",
            LinkedInProfileUrl = $"https://uk.linkedin.com/in/{_PROFILE_ID}",
            WebSites = new List<PersonWebsite> { new PersonWebsite { Url = "https://contoso.com/ceo-profile" } }
        };

        private readonly Person m_Person;
        private readonly Shared.Infrastructure.DataPoolApi.Models.Person.Person m_DataPoolPerson;

        private readonly List<Person> m_StoredPersons = new List<Person>();

        private readonly FakeCosmos m_FakeCosmos;
        private readonly FakeStorageQueue m_FakeStorageQueue = new FakeStorageQueue();

        private readonly Mock<IDataPoolApi> m_DataPoolApiMock = new Mock<IDataPoolApi>();

        private readonly Mock<IRepository> m_RepositoryMock = new Mock<IRepository>();
        private readonly Mock<IPersonInfrastructure> m_PersonInfrastructure = new Mock<IPersonInfrastructure>();

        public PutTests()
        {


            m_Person = new Person(m_SearchFirmId, linkedInProfileUrl: m_Command.LinkedInProfileUrl);
            m_Command.Id = m_Person.Id;

            m_StoredPersons.Add(m_Person);

            m_FakeCosmos = new FakeCosmos()
                          .EnableContainerFetch(FakeCosmos.PersonsContainerName, m_Person.Id.ToString(), m_SearchFirmId.ToString(), () => m_Person)
                          .EnableContainerReplace<Person>(FakeCosmos.PersonsContainerName, m_Person.Id.ToString(), m_SearchFirmId.ToString())
                          .EnableContainerLinqQuery(FakeCosmos.PersonsContainerName, m_SearchFirmId.ToString(), () => m_StoredPersons);

            m_DataPoolPerson = new Shared.Infrastructure.DataPoolApi.Models.Person.Person
            {
                PersonDetails = new PersonDetails
                {
                    Name = "Data pool name",
                    PhotoUrl = "https://photo.url/data-pool-person",
                    Biography = "I was born shining!"
                },
                Location = new Address
                {
                    StreetName = "123 Perfect Road",
                    Country = "United Kingdom",
                    CountryCodeISO3 = "GB",
                    Municipality = "Bright City",
                    ExtendedPostalCode = "AA11 9ZZ",
                    GeoLocation = new EdmGeographyPoint(-1, -1)
                },
                WebsiteLinks = new List<WebLink> { new WebLink { Url = "https://twitter.com/myawsomeprofile", LinkTo = Linkage.Twitter } },
                CurrentEmployment = new Job { CompanyName = "Awsome Ltd", Position = "Senior CEO :)" },
                PreviousEmployment = new List<Job> { new Job { CompanyName = "123 Fun" } },
                ScrapedPersonFromGoogle = new ScrapedPersonFromGoogle { Title = "Some data" }
            };

            m_DataPoolApiMock
               .Setup(dp => dp.Get(It.Is<string>(id => id == m_DataPoolPerson.Id.ToString()), It.IsAny<CancellationToken>()))
               .ReturnsAsync(m_DataPoolPerson);


            m_RepositoryMock.Setup(r => r.GetByQuery(It.IsAny<Expression<Func<Person, bool>>>()))
                .Returns(Task.FromResult(new List<Person>()
                {
                    m_Person
                }));

        }

        [Fact]
        public async Task PutUpdatesItemInContainer()
        {
            // Given
            var controller = CreateController();

            // When
            var actionResult = await controller.Put(m_Person.Id, m_Command);

            // Then
            var container = m_FakeCosmos.PersonsContainer;

            var result = ((Put.Result)((OkObjectResult)actionResult).Value).LocalPerson;

            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(i => i.Id == result.Id &&
                                                                        i.LinkedInProfileId == _PROFILE_ID &&
                                                                        i.CreatedDate.Date == DateTime.UtcNow.Date &&
                                                                        i.SearchFirmId == m_SearchFirmId &&
                                                                        i.Name == m_Command.Name &&
                                                                        i.JobTitle == m_Command.JobTitle &&
                                                                        i.Location == m_Command.Location &&
                                                                        i.TaggedEmails.AssertSameList(m_Command.TaggedEmails) &&
                                                                        i.PhoneNumbers.IsSameList(m_Command.PhoneNumbers) &&
                                                                        i.Organisation == m_Command.Company &&
                                                                        i.Bio == m_Command.Bio &&
                                                                        i.LinkedInProfileUrl == m_Command.LinkedInProfileUrl
                                                                        && i.WebSites.Exists(w => w.Url == m_Command.WebSites[0].Url &&
                                                                                            w.Type == WebSiteType.Other)),
                                                     It.Is<string>(id => id == m_Person.Id.ToString()),
                                                     It.Is<PartitionKey>(p => p == new PartitionKey(m_SearchFirmId.ToString())),
                                                     It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task PutDoesNotSetBioToNullIfSetAlready()
        {
            const string oldBio = "a bio";
            // Given
            m_Person.Bio = oldBio;
            m_Command.Bio = null;
            var controller = CreateController();

            // When
            var actionResult = await controller.Put(m_Person.Id, m_Command);

            // Then
            var container = m_FakeCosmos.PersonsContainer;

            var result = ((Put.Result)((OkObjectResult)actionResult).Value).LocalPerson;

            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(i => i.Id == result.Id &&
                                                                        i.LinkedInProfileId == _PROFILE_ID &&
                                                                        i.CreatedDate.Date == DateTime.UtcNow.Date &&
                                                                        i.SearchFirmId == m_SearchFirmId &&
                                                                        i.Name == m_Command.Name &&
                                                                        i.JobTitle == m_Command.JobTitle &&
                                                                        i.Location == m_Command.Location &&
                                                                        i.TaggedEmails.AssertSameList(m_Command.TaggedEmails) &&
                                                                        i.PhoneNumbers.IsSameList(m_Command.PhoneNumbers) &&
                                                                        i.Organisation == m_Command.Company &&
                                                                        i.Bio == oldBio &&
                                                                        i.LinkedInProfileUrl == m_Command.LinkedInProfileUrl
                                                                        && i.WebSites.Exists(w => w.Url == m_Command.WebSites[0].Url &&
                                                                                            w.Type == WebSiteType.Other)),
                                                     It.Is<string>(id => id == m_Person.Id.ToString()),
                                                     It.Is<PartitionKey>(p => p == new PartitionKey(m_SearchFirmId.ToString())),
                                                     It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task PutDoesNotThrowWhenLocationIsNull() 
        {
            // Given
            m_DataPoolPerson.Location = null;
            m_Person.DataPoolPersonId = m_DataPoolPerson.Id;
            var controller = CreateController();

            // When
            var ex = await Record.ExceptionAsync(() => controller.Put(m_Person.Id, m_Command));

            // Then
            Assert.Null(ex);
        }

        [Theory]
        [ClassData(typeof(ValidLinkedInProfileUrlNormalisationsIncludingRedirect))]
        public async Task PutUpdatesItemInContainerWithNormalisedProfileId(string profileUrl, string expectedNormalisedProfileId)
        {
            // Given
            m_Command.LinkedInProfileUrl = profileUrl;
            var controller = CreateController();

            // When
            await controller.Put(m_Person.Id, m_Command);

            // Then
            var container = m_FakeCosmos.PersonsContainer;
            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(i => i.LinkedInProfileId == expectedNormalisedProfileId), It.IsAny<string>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
        }

        [Theory]
        [ClassData(typeof(UnpopulatedStrings))]
        public async Task PutUpdatesItemInContainerWithNormalisedProfileIdWhenEmptyProfileUrl(string emptyProfileUrl)
        {
            // Given
            m_Command.LinkedInProfileUrl = emptyProfileUrl;
            var controller = CreateController();

            // When
            await controller.Put(m_Person.Id, m_Command);

            // Then
            var container = m_FakeCosmos.PersonsContainer;

            Guid unused;
            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(i => i.LinkedInProfileId.StartsWith("Empty-") &&
                                                                        Guid.TryParse(i.LinkedInProfileId.Replace("Empty-", ""), out unused)), It.IsAny<string>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task PutUpdatesItemInContainerWithNewUrlSameProfileId()
        {
            // Given
            m_Command.LinkedInProfileUrl += "/in";
            var controller = CreateController();

            // When
            await controller.Put(m_Person.Id, m_Command);

            // Then
            var container = m_FakeCosmos.PersonsContainer;
            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(i => i.LinkedInProfileId == _PROFILE_ID), It.IsAny<string>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(i => i.LinkedInProfileUrl == m_Command.LinkedInProfileUrl), It.IsAny<string>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
        }

        [Theory, CombinatorialData]
        public async Task PutReturnsUpdatedResource(bool isPresentInDataPool)
        {
            // Given
            var controller = CreateController();
            if (isPresentInDataPool)
                m_Person.DataPoolPersonId = m_DataPoolPerson.Id;

            // When
            var actionResult = await controller.Put(m_Person.Id, m_Command);

            // Then
            var result = ((Put.Result)((OkObjectResult)actionResult).Value);
            var localPerson = result.LocalPerson;

            Assert.Equal(m_Command.Id, localPerson.Id);
            Assert.Equal(m_Command.Name, localPerson.Name);
            Assert.Equal(m_Command.JobTitle, localPerson.JobTitle);
            Assert.Equal(m_Command.Location, localPerson.Location);
            Assert.All(m_Command.TaggedEmails, e => Assert.Contains(localPerson.TaggedEmails, tr => e.Email == tr.Email && e.SmtpValid == tr.SmtpValid));
            Assert.Equal(m_Command.PhoneNumbers, localPerson.PhoneNumbers);
            Assert.Equal(m_Command.Company, localPerson.Company);
            Assert.Equal(m_Command.LinkedInProfileUrl, localPerson.LinkedInProfileUrl);
            Assert.True(localPerson.WebSites.Exists(w => w.Url == m_Command.WebSites[0].Url &&
                                                    w.Type == WebSiteType.Other));

            if (isPresentInDataPool)
            {
                var dataPoolPerson = result.DataPoolPerson;

                Assert.Equal(m_DataPoolPerson.Id, dataPoolPerson.Id);
                Assert.Equal(m_DataPoolPerson.PersonDetails.Name, dataPoolPerson.Name);
                Assert.Equal(m_DataPoolPerson.CurrentEmployment.Position, dataPoolPerson.JobTitle);
                Assert.Equal($"{m_DataPoolPerson.Location.Municipality}, {m_DataPoolPerson.Location.Country}", dataPoolPerson.Location);
                Assert.Equal(m_DataPoolPerson.CurrentEmployment.CompanyName, dataPoolPerson.Company);

                Assert.Equal(m_DataPoolPerson.WebsiteLinks[0].Url, dataPoolPerson.WebSites[0].Url);
                Assert.Equal(WebSiteType.Twitter, dataPoolPerson.WebSites[0].Type); //hard-coded, refactor when have time
            }
        }

        public static IEnumerable<object[]> WebSitesTestData()
        {
            yield return new object[] { new List<PersonWebsite>(), new List<PersonWebsite>() };
            yield return new object[] { null, new List<PersonWebsite>() };
            yield return new object[]
                         {
                             new List<PersonWebsite>
                             {
                                 new PersonWebsite { Url = "https://www.random.web.site/companies/talentis" },
                                 new PersonWebsite { Url = "http://another.profile.gov.uk/company/12345678" },
                                 new PersonWebsite { Url = "https://companycheck.co.uk/company/01234567/TALENTIS--COMPANY-UNLIMITED/companies-house-data" }
                             },
                             new List<PersonWebsite>
                             {
                                 new PersonWebsite { Url = "https://www.random.web.site/companies/talentis", Type = WebSiteType.Other },
                                 new PersonWebsite { Url = "http://another.profile.gov.uk/company/12345678", Type = WebSiteType.Other },
                                 new PersonWebsite { Url = "https://companycheck.co.uk/company/01234567/TALENTIS--COMPANY-UNLIMITED/companies-house-data", Type = WebSiteType.Other }
                             }
                         };
            yield return new object[]
                         {
                             new List<PersonWebsite>
                             {
                                 new PersonWebsite { Url = "https://www.crunchbase.com/organization/talentis" },
                                 new PersonWebsite { Url = "https://beta.companieshouse.gov.uk/company/12345678" },
                                 new PersonWebsite { Url = "https://companycheck.co.uk/company/01234567/TALENTIS--COMPANY-UNLIMITED/companies-house-data" },
                                 new PersonWebsite { Url = "https://www.xing.com/profile/Arthur_Conan_Doyle/cv" },
                                 new PersonWebsite { Url = "https://www.facebook.com/public/talentis" },
                                 new PersonWebsite { Url = "https://twitter.com/talentis-ceo" },
                                 new PersonWebsite { Url = "https://www.owler.com/company/talentis" },
                                 new PersonWebsite { Url = "https://www.linkedin.com/in/talentis-test-profile" },
                                 new PersonWebsite { Url = "https://www.youtube.com/c/talentis" },
                                 new PersonWebsite { Url = "https://www.bloomberg.com/profile/company/talentis" },
                                 new PersonWebsite { Url = "https://www.zoominfo.com/c/talentis-corporation/12345678" },
                                 new PersonWebsite { Url = "HTTP://WWW.TWITTER.COM/talentis-ceo" },
                                 new PersonWebsite { Url = "https://www.reed.co.uk/jobs/talentis-12345/p98765" },
                                 new PersonWebsite { Url = "https://www.reuters.com/companies/talentis" },
                                 new PersonWebsite { Url = "http://youtube.com/c/talentis-2" }
                             },
                             new List<PersonWebsite>
                             {
                                 new PersonWebsite { Url = "https://www.linkedin.com/in/talentis-test-profile", Type = WebSiteType.LinkedIn },
                                 new PersonWebsite { Url = "https://www.xing.com/profile/Arthur_Conan_Doyle/cv", Type = WebSiteType.Xing },
                                 new PersonWebsite { Url = "https://www.crunchbase.com/organization/talentis", Type = WebSiteType.Crunchbase },
                                 new PersonWebsite { Url = "https://www.reuters.com/companies/talentis", Type = WebSiteType.Reuters },
                                 new PersonWebsite { Url = "https://www.bloomberg.com/profile/company/talentis", Type = WebSiteType.Bloomberg },
                                 new PersonWebsite { Url = "https://www.zoominfo.com/c/talentis-corporation/12345678", Type = WebSiteType.ZoomInfo },
                                 new PersonWebsite { Url = "https://twitter.com/talentis-ceo", Type = WebSiteType.Twitter },
                                 new PersonWebsite { Url = "HTTP://WWW.TWITTER.COM/talentis-ceo", Type = WebSiteType.Twitter },
                                 new PersonWebsite { Url = "https://www.owler.com/company/talentis", Type = WebSiteType.Owler },
                                 new PersonWebsite { Url = "https://beta.companieshouse.gov.uk/company/12345678", Type = WebSiteType.CompaniesHouse },
                                 new PersonWebsite { Url = "https://www.youtube.com/c/talentis", Type = WebSiteType.YouTube },
                                 new PersonWebsite { Url = "http://youtube.com/c/talentis-2", Type = WebSiteType.YouTube },
                                 new PersonWebsite { Url = "https://www.facebook.com/public/talentis", Type = WebSiteType.Facebook },
                                 new PersonWebsite { Url = "https://companycheck.co.uk/company/01234567/TALENTIS--COMPANY-UNLIMITED/companies-house-data", Type = WebSiteType.Other },
                                 new PersonWebsite { Url = "https://www.reed.co.uk/jobs/talentis-12345/p98765", Type = WebSiteType.Other }
                             }
                         };
            yield return new object[] { null, null };
        }

        [Theory]
        [MemberData(nameof(WebSitesTestData))]
        public async Task PutSetsAndReturnsCorrectWebSites(List<PersonWebsite> webSites, List<PersonWebsite> expectedWebSites)
        {
            // Given
            m_Command.WebSites = webSites;
            var controller = CreateController();

            // When
            var actionResult = await controller.Put(m_Person.Id, m_Command);

            // Then
            var result = ((Put.Result)((OkObjectResult)actionResult).Value).LocalPerson;

            if (expectedWebSites != null)
            {
                m_FakeCosmos.PersonsContainer
                            .Verify(c => c.ReplaceItemAsync(It.Is<Person>(i => i.WebSites.SourceItemsExistInTarget(expectedWebSites, (d1, d2) =>
                                                                                                                                         d1.Url == d2.Url &&
                                                                                                                                         d1.Type == d2.Type)),
                                                            It.IsAny<string>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(),
                                                            It.IsAny<CancellationToken>()));

                Assert.True(result.WebSites.SourceItemsExistInTarget(expectedWebSites, (r, d) => r.Url == d.Url && r.Type == d.Type));
            }
            else
            {
                m_FakeCosmos.PersonsContainer
                            .Verify(c => c.ReplaceItemAsync(It.Is<Person>(i => i.WebSites == null || !i.WebSites.Any()),
                                                            It.IsAny<string>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(),
                                                            It.IsAny<CancellationToken>()));

                Assert.Empty(result.WebSites);
            }
        }

        [Theory]
        [ClassData(typeof(ValidLinkedInProfileUrlNormalisationsIncludingRedirect))]
        public async Task PutThrowsWhenPersonExistsWithSameProfileId(string requestLinkedInProfileUrl, string existingRecordLinkedInProfileId)
        {
            // Given
            m_Command.LinkedInProfileUrl = requestLinkedInProfileUrl;
            m_StoredPersons.Add(new Person(m_SearchFirmId, linkedInProfileUrl: $"https://www.linkedin.com/in/{existingRecordLinkedInProfileId}"));
            var controller = CreateController();

            // When
            var ex = await Record.ExceptionAsync(() => controller.Put(m_Person.Id, m_Command));

            // Then
            ex.AssertParamValidationFailure(nameof(Post.Command.LinkedInProfileUrl), "A record already exists with this {Param}");
        }


        [Theory]
        [ClassData(typeof(PersonLocationChangedValues))]
        public async Task PutQueuesChangedLocationMessageIfLocationChanged(string existingLocation, string newLocation)
        {
            // Given
            m_Person.Location = existingLocation;
            m_Command.Location = newLocation;
            var controller = CreateController();

            // When
            await controller.Put(m_Person.Id, m_Command);

            // Then
            var queuedItem = m_FakeStorageQueue.GetQueuedItem<PersonLocationChangedQueueItem>(QueueStorage.QueueNames.PersonLocationChangedQueue);
            Assert.Equal(m_Person.Id, queuedItem.PersonId);
            Assert.Equal(m_SearchFirmId, queuedItem.SearchFirmId);
        }

        [Theory]
        [ClassData(typeof(PersonLocationNotChangedValues))]
        public async Task PutDoesNotQueueChangedLocationMessageIfLocationUnchanged(string existingLocation, string newLocation)
        {
            // Given
            m_Person.Location = existingLocation;
            m_Command.Location = newLocation;
            var controller = CreateController();

            // When
            await controller.Put(m_Person.Id, m_Command);

            // Then
            Assert.Equal(0, m_FakeStorageQueue.GetQueuedItemCount(QueueStorage.QueueNames.PersonLocationChangedQueue));
        }

        private PersonsController CreateController()
            => new ControllerBuilder<PersonsController>()
              .SetSearchFirmUser(m_SearchFirmId)
              .SetFakeCosmos(m_FakeCosmos)
              .SetFakeCloudQueue(m_FakeStorageQueue)
               .AddTransient(m_RepositoryMock.Object)
                .AddTransient(m_PersonInfrastructure.Object)
              .AddTransient(m_DataPoolApiMock.Object)
              .Build();
    }
}
