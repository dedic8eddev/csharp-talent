using Ikiru.Parsnips.Api.Controllers.Persons;
using Ikiru.Parsnips.Application.Infrastructure.DataPool;
using Ikiru.Parsnips.Application.Services.DataPool;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
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
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using DataPoolModelPerson = Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi.Models.Person;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Persons
{
    public class GetListTests
    {
        private readonly Guid m_SearchFirmId = Guid.NewGuid();
        private readonly FakeCosmos m_FakeCosmos = new FakeCosmos();

        private GetList.Query m_Query = new GetList.Query();

        private string m_StoredLinkedInProfileId = "default-unit-test-profile-id";
        private string StoredLinkedInProfileUrl => $"https://www.linkedin.com/in/{m_StoredLinkedInProfileId}";

        private List<Person> m_StoredPersons;

        private readonly Mock<IDataPoolService> m_DataPoolServiceMock;
        private readonly DataPoolModelPerson.Person m_DataPoolModelPerson;
        private readonly Guid m_DataPoolId = new Guid("d471b305-1ec9-4aef-b9f4-5101c02c32fa");
        private readonly Mock<IRepository> m_RepositoryMock = new Mock<IRepository>();
        private readonly Mock<IPersonInfrastructure> m_PersonInfrastructure = new Mock<IPersonInfrastructure>();
        public GetListTests()
        {
            m_DataPoolModelPerson = new DataPoolModelPerson.Person
            {
                Id = m_DataPoolId
            };

            m_DataPoolServiceMock = new Mock<IDataPoolService>();

            m_DataPoolServiceMock.Setup(x => x.GetSinglePersonById(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(m_DataPoolModelPerson));

            m_StoredPersons = new List<Person>();
            m_FakeCosmos.EnableContainerLinqQuery(FakeCosmos.PersonsContainerName, m_SearchFirmId.ToString(), () => m_StoredPersons); // Deferred
        }

        private void SeedPersonQuery()
        {
            // This can't be done in constructor as Stored Profile Url needs to be altered for some tests
            m_StoredPersons.AddRange(new[]
                                        {
                                            new Person(m_SearchFirmId, null, StoredLinkedInProfileUrl)
                                            {
                                                Name = "GetList 'B'",
                                                JobTitle = "Test Subject One",
                                                Location = "Location One",
                                                TaggedEmails = new List<TaggedEmail> { new TaggedEmail { Email = "getlist-item-1@parsnips.com" }, new TaggedEmail { Email = "additional-1@parsnips.com" } },
                                                PhoneNumbers = new List<string>{ "01111 000000" },
                                                Organisation = "Company One",
                                                WebSites = new List<PersonWebsite> { new PersonWebsite { Url = "https://www.youtube.com/c/talentis", Type = WebSiteType.YouTube }, new PersonWebsite { Url = "https://example.com/profiles/talentis", Type = WebSiteType.Other } },
                                                DataPoolPersonId = m_DataPoolId
                                            },
                                            new Person(m_SearchFirmId)
                                            {
                                                Name = "GetList 'C'",
                                                JobTitle = "Test Subject Two",
                                                Location = "Location Two",
                                                TaggedEmails = new List<TaggedEmail> { new TaggedEmail { Email = "getlist-item-2@parsnips.com" }, new TaggedEmail { Email = "additional-2@parsnips.com" } },
                                                PhoneNumbers = new List<string>{ "02222 000000" },
                                                Organisation = "Company Two"
                                            },
                                            new Person(m_SearchFirmId)
                                            {
                                                Name = "GetList 'A' Dupe",
                                                JobTitle = "Test Subject Three",
                                                Location = "Location Three",
                                                TaggedEmails = new List<TaggedEmail> { new TaggedEmail { Email = "getlist-item-3@parsnips.com" }, new TaggedEmail { Email = "additional-3@parsnips.com" } },
                                                PhoneNumbers = new List<string>{ "03333 000000" },
                                                Organisation = "Company Three"
                                            },
                                            new Person(m_SearchFirmId)
                                            {
                                                Name = "GetList 'A' Dupe",
                                                JobTitle = "Test Subject Four",
                                                Location = "Location Four",
                                                TaggedEmails = new List<TaggedEmail> { new TaggedEmail { Email = "getlist-item-4@parsnips.com" }, new TaggedEmail { Email = "additional-4@parsnips.com" } },
                                                PhoneNumbers = new List<string>{ "04444 000000" },
                                                Organisation = "Company Four"
                                            }
                                        });
        }

        [Theory]
        [ClassData(typeof(ValidLinkedInProfileUrlNormalisations))]
        public async Task GetListReturnsCorrectResultByLinkedInProfileId(string validLinkedInProfileUrl, string profileId)
        {
            // Given
            m_StoredLinkedInProfileId = profileId;
            m_Query.LinkedInProfileUrl = validLinkedInProfileUrl;
            var controller = CreateController();

            // When
            var actionResult = await controller.GetList(m_Query);

            // Then
            var result = (GetList.Result)((OkObjectResult)actionResult).Value;
            var resultPerson = Assert.Single(result.Persons);
            var personOne = m_StoredPersons[0];
            // ReSharper disable PossibleNullReferenceException
            Assert.Equal(personOne.Id, resultPerson.LocalPerson.Id);
            Assert.Equal(personOne.Name, resultPerson.LocalPerson.Name);
            Assert.Equal(personOne.JobTitle, resultPerson.LocalPerson.JobTitle);
            Assert.Equal(personOne.Location, resultPerson.LocalPerson.Location);
            personOne.TaggedEmails.AssertSameList(resultPerson.LocalPerson.TaggedEmails);
            Assert.Equal(personOne.PhoneNumbers, resultPerson.LocalPerson.PhoneNumbers);
            Assert.Equal(personOne.Organisation, resultPerson.LocalPerson.Company);
            Assert.Equal(personOne.LinkedInProfileUrl, resultPerson.LocalPerson.LinkedInProfileUrl);
            Assert.True(personOne.WebSites.IsSameList(resultPerson.LocalPerson.WebSites, (d, a) => d.Url == a.Url && d.Type == a.Type));
            // ReSharper restore PossibleNullReferenceException
        }

        public class MatchingTestCases : BaseTestDataSource
        {
            protected override IEnumerator<object[]> GetValues()
            {
                /* Singles */
                yield return new object[] { new Action<GetList.Query>(q => q.Name = "GetList 'C'"), new[] { 1 } };
                yield return new object[] { new Action<GetList.Query>(q => q.Email = "getlist-item-4@parsnips.com"), new[] { 3 } };
                yield return new object[] { new Action<GetList.Query>(q =>
                                                                      {
                                                                          q.Name = "GetList 'B'";
                                                                          q.Email = "additional-1@parsnips.com";
                                                                      }),  new[] { 0 } };
                yield return new object[] { new Action<GetList.Query>(q =>
                                                                      {
                                                                          q.LinkedInProfileUrl = "https://www.linkedin.com/in/default-unit-test-profile-id";
                                                                          q.Name = "GetList 'B'";
                                                                          q.Email = "getlist-item-1@parsnips.com";
                                                                      }),  new[] { 0 } };
                /* Multiples */
                yield return new object[] { new Action<GetList.Query>(q => q.Name = "GetList 'A' Dupe"), new[] { 2, 3 } };
                yield return new object[] { new Action<GetList.Query>(q =>
                                                                      {
                                                                          q.Name = "GetList 'C'";
                                                                          q.Email = "getlist-item-4@parsnips.com";
                                                                      }),  new[] { 3, 1 } };
                yield return new object[] { new Action<GetList.Query>(q =>
                                                                      {
                                                                          q.LinkedInProfileUrl = "https://www.linkedin.com/in/default-unit-test-profile-id";
                                                                          q.Name = "GetList 'C'";
                                                                          q.Email = "getlist-item-4@parsnips.com";
                                                                      }),  new[] { 3, 0, 1 } };
                yield return new object[] { new Action<GetList.Query>(q =>
                                                                      {
                                                                          q.Name = "GetList 'A' Dupe";
                                                                          q.Email = "getlist-item-1@parsnips.com";
                                                                      }),  new[] { 2, 3, 0 } };
            }
        }

        [Theory]
        [ClassData(typeof(MatchingTestCases))]
        public async Task GetListReturnsCorrectResults(Action<GetList.Query> querySetter, int[] storedPersonIndexesExpectedInOrder)
        {
            // Given
            querySetter(m_Query);
            var controller = CreateController();

            // When
            var actionResult = await controller.GetList(m_Query);

            // Then
            var result = (GetList.Result)((OkObjectResult)actionResult).Value;

            Assert.Equal(storedPersonIndexesExpectedInOrder.Length, result.Persons.Count);

            var i = 0;
            foreach (var resultPerson in result.Persons)
            {
                var storedPerson = m_StoredPersons[storedPersonIndexesExpectedInOrder[i++]];
                Assert.Equal(storedPerson.Id, resultPerson.LocalPerson.Id);
                Assert.Equal(storedPerson.Name, resultPerson.LocalPerson.Name);
                Assert.Equal(storedPerson.JobTitle, resultPerson.LocalPerson.JobTitle);
                Assert.Equal(storedPerson.Location, resultPerson.LocalPerson.Location);
                storedPerson.TaggedEmails.AssertSameList(resultPerson.LocalPerson.TaggedEmails);
                Assert.Equal(storedPerson.PhoneNumbers, resultPerson.LocalPerson.PhoneNumbers);
                Assert.Equal(storedPerson.Organisation, resultPerson.LocalPerson.Company);
                Assert.Equal(storedPerson.LinkedInProfileUrl, resultPerson.LocalPerson.LinkedInProfileUrl);
                Assert.True(storedPerson.WebSites.IsSameList(resultPerson.LocalPerson.WebSites, (d, r) => d.Url == r.Url && d.Type == r.Type));

                if (storedPerson.DataPoolPersonId != null)
                {
                    m_DataPoolServiceMock.Verify(x => x.GetSinglePersonById(It.Is<string>(dp => dp == storedPerson.DataPoolPersonId.ToString()), It.IsAny<CancellationToken>()),
                        Times.Once());

                    Assert.Equal(storedPerson.DataPoolPersonId, resultPerson.DataPoolPerson.DataPoolId);
                }
                else
                {
                    m_DataPoolServiceMock.Verify(x => x.GetSinglePersonById(It.Is<string>(dp => dp == storedPerson.DataPoolPersonId.ToString()), It.IsAny<CancellationToken>()),
                        Times.Never());
                }
            }
        }

        [Fact]
        public async Task GetListReturnsLimitedResults()
        {
            // Given
            const int max = 10;
            m_Query.Name = "Dave GetList";
            m_StoredPersons = Enumerable.Range(0, max + 1).Select(_ => new Person(m_SearchFirmId) { Name = m_Query.Name }).ToList();
            var controller = CreateController();

            // When
            await controller.GetList(m_Query);

            // Then
            var container = m_FakeCosmos.PersonsContainer;
            // MaxItemSize on Fake implementation doesn't work, so just have to verify it was called as expected
            container.Verify(c => c.GetItemLinqQueryable<Person>(It.IsAny<bool>(), It.IsAny<string>(), It.Is<QueryRequestOptions>(o => o.MaxItemCount == max), It.IsAny<CosmosLinqSerializerOptions>())); // Called with page limit
            container.Verify(c => c.GetItemLinqQueryable<Person>(It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<QueryRequestOptions>(), It.IsAny<CosmosLinqSerializerOptions>()), Times.Once); // Only called once
        }

        [Fact]
        public async Task GetListReturnsEmpty()
        {
            // Given
            m_Query.LinkedInProfileUrl = "https://www.linkedin.com/in/doesnt-exist-in-data-store";
            var controller = CreateController();

            // When
            var actionResult = await controller.GetList(m_Query);

            // Then
            var result = (GetList.Result)((OkObjectResult)actionResult).Value;
            Assert.Empty(result.Persons);
        }


        private PersonsController CreateController()
        {
            SeedPersonQuery();

            return new ControllerBuilder<PersonsController>()
                .SetFakeCosmos(m_FakeCosmos)
                .AddTransient(m_DataPoolServiceMock.Object)
                .AddTransient(m_RepositoryMock.Object)
                .AddTransient(m_PersonInfrastructure.Object)                  
                .SetSearchFirmUser(m_SearchFirmId)
                .Build();
        }
    }
}
