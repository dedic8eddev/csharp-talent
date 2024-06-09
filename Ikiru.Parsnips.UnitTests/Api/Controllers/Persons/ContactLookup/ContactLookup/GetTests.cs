using Ikiru.Parsnips.Api.Controllers.Persons.ContactLookup.ContactLookupDetails;
using Ikiru.Parsnips.Api.RocketReach;
using Ikiru.Parsnips.Api.RocketReach.Models;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ResourceNotFound;
using Ikiru.Parsnips.UnitTests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ikiru.Parsnips.Domain.Enums;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Persons.ContactLookup.ContactLookup
{
    public class GetTests
    {
        private readonly string m_linkedinProfileUrl = "https://www.linkedin.com/in/voyager-training-219196134/";
        private readonly Mock<IRocketReachApi> m_RocketReachApi;
        private readonly Person m_Person;
        private readonly Guid _searchFirmId;
        private readonly SearchFirm m_SearchFirm;
        private Get.Query m_Query;
        private const string _ROCKET_REACH_API_KEY = "testkey";
        private readonly FakeCosmos m_FakeCosmos;
        private readonly Person m_PersonWithoutLinkedInUrl;
        private readonly Person m_PersonMissingResults;
        private readonly Person m_PersonInvalidEmailResults;
        private readonly Person m_PersonPreviouslyFetchedEmails;
        private readonly Person m_PersonException;
        private readonly LookupProfileResponseModel m_LookupProfileResponseModel;
        private readonly SearchFirmToken m_TokenPlan;
        private readonly SearchFirmToken m_TokenPurchase;
        private readonly Guid m_SearchFirmUserId = Guid.NewGuid();
        private readonly RocketReachSettings _rocketReachSettings = new RocketReachSettings
        {
            ApiKey = _ROCKET_REACH_API_KEY,
            BaseUrl = "",
            BypassCredits = false,
            DelayBetweenRetriesMilliseconds = 10,
            RetryNumber = 3
        };
        private readonly FakeRepository _fakeRepository = new FakeRepository();

        public GetTests()
        {
            m_RocketReachApi = new Mock<IRocketReachApi>();

            m_SearchFirm = new SearchFirm
            {
                Name = "Test Company Name"
            };
            _searchFirmId = m_SearchFirm.Id;

            m_Person = new Person(m_SearchFirm.SearchFirmId, linkedInProfileUrl: m_linkedinProfileUrl)
            {
                Organisation = "test org 1",
                Name = "Person Name"
            };

            m_PersonException = new Person(m_SearchFirm.SearchFirmId, linkedInProfileUrl: "https://linkedin.com/in/exception")
            {
                Organisation = " exception test org 1",
                Name = "Person exception"
            };

            m_PersonMissingResults = new Person(_searchFirmId, linkedInProfileUrl: "https://linkedin.com/in/missingprofile")
            {
                Organisation = "no results test org 1",
                Name = "Person Name no results"
            };

            m_PersonWithoutLinkedInUrl = new Person(_searchFirmId)
            {
                Organisation = " without linkedin url results test org 1",
                Name = "Person without linkedin url"
            };

            m_PersonPreviouslyFetchedEmails = new Person(_searchFirmId, linkedInProfileUrl: "https://linkedin.com/in/previosufetchedemials")
            {
                Organisation = "fetched emails test org 1",
                RocketReachFetchedInformation = true,
                Name = "Person previously fetched emails"
            };

            m_PersonInvalidEmailResults = new Person(m_SearchFirm.Id, linkedInProfileUrl: "https://linkedin.com/in/invalidsmtp")
            {
                Organisation = "fetched emails test org 1",
                RocketReachFetchedInformation = true,
                Name = "Person previously fetched emails"
            };

            var personsEmails = new[]
                                {
                                    "john.smith@google.com", "Tom.Jones@hotmail.com", "test text", "inconclusive@email.com", "invalid@email.com",
                                    "unverified@gmail.com"
                                };

            m_LookupProfileResponseModel = new LookupProfileResponseModel
            {
                status = "complete",
                emails = new[]
                {
                    new LookupProfileResponseEmailModel { email = personsEmails[0], smtp_valid = "valid"},
                    new LookupProfileResponseEmailModel { email = personsEmails[1], smtp_valid = "valid" },
                    new LookupProfileResponseEmailModel { email = personsEmails[3], smtp_valid = "inconclusive" },
                    new LookupProfileResponseEmailModel { email = personsEmails[4], smtp_valid = "invalid" },
                    new LookupProfileResponseEmailModel { email = personsEmails[5], smtp_valid = "unverified" }
                },
                phones = new[]
                {
                    new LookupProfileResponsePhoneNumberModel { number = "0123-456-789", type = "mobile"  },
                    new LookupProfileResponsePhoneNumberModel { number = "444-555-888", type = "professional"  },
                    new LookupProfileResponsePhoneNumberModel { number = "3333-888-789", type = "unkown"  }
                }
            };

            m_RocketReachApi.Setup(x => x.LookupProfile(It.Is<string>(a => a == _ROCKET_REACH_API_KEY),
                                                        It.Is<string>(x => x == m_Person.LinkedInProfileUrl)))
                            .Returns<string, string>((a, b) => Task.FromResult(m_LookupProfileResponseModel));


            m_RocketReachApi.Setup(x => x.LookupProfile(It.Is<string>(a => a == _ROCKET_REACH_API_KEY),
                                                        It.Is<string>(x => x == m_PersonWithoutLinkedInUrl.LinkedInProfileUrl)))
                            .Returns<string, string>((a, b) => Task.FromResult(m_LookupProfileResponseModel));

            m_RocketReachApi.Setup(x => x.LookupProfile(It.Is<string>(a => a == _ROCKET_REACH_API_KEY),
                                                        It.Is<string>(x => x == m_PersonWithoutLinkedInUrl.Name),
                                                        It.Is<string>(x => x == m_PersonWithoutLinkedInUrl.Organisation)))
                            .Returns<string, string, string>((a, b, c) => Task.FromResult(m_LookupProfileResponseModel));

            m_RocketReachApi.Setup(x => x.LookupProfile(It.Is<string>(a => a == _ROCKET_REACH_API_KEY),
                                                        It.Is<string>(x => x == m_PersonMissingResults.LinkedInProfileUrl)))
                            .Returns<string, string>((a, b) => Task.FromResult(new LookupProfileResponseModel { status = "complete" }));

            m_RocketReachApi.Setup(x => x.LookupProfile(It.Is<string>(a => a == _ROCKET_REACH_API_KEY),
                                                        It.Is<string>(x => x == m_PersonMissingResults.Name),
                                                        It.Is<string>(x => x == m_PersonMissingResults.Organisation)))
                            .Returns<string, string, string>((a, b, c) => Task.FromResult(new LookupProfileResponseModel { status = "complete" }));

            m_RocketReachApi.Setup(x => x.LookupProfile(It.Is<string>(a => a == _ROCKET_REACH_API_KEY),
                                                        It.Is<string>(x => x == m_PersonException.Name),
                                                        It.Is<string>(x => x == m_PersonMissingResults.Organisation)))
                            .Throws<Exception>();

            m_RocketReachApi.Setup(x => x.LookupProfile(It.Is<string>(a => a == _ROCKET_REACH_API_KEY),
                                                        It.Is<string>(x => x == m_PersonException.LinkedInProfileUrl)))
                            .Throws<Exception>();


            m_RocketReachApi.Setup(x => x.LookupProfile(It.Is<string>(a => a == _ROCKET_REACH_API_KEY),
                                                       It.Is<string>(x => x == m_PersonInvalidEmailResults.LinkedInProfileUrl)))
                           .Returns<string, string>((a, b) => Task.FromResult(new LookupProfileResponseModel
                           {
                               phones = new LookupProfileResponsePhoneNumberModel[]
                                {},
                               emails = new LookupProfileResponseEmailModel[]
                               {
                                   new LookupProfileResponseEmailModel()
                                   {
                                       smtp_valid = "invalid"
                                   }
                               }
                           }));

            m_TokenPlan = new SearchFirmToken(_searchFirmId, DateTimeOffset.Now.AddDays(3), Domain.Enums.TokenOriginType.Plan);
            m_TokenPurchase = new SearchFirmToken(_searchFirmId, DateTimeOffset.Now.AddDays(2), Domain.Enums.TokenOriginType.Purchase);
            var token1 = new SearchFirmToken(_searchFirmId, DateTimeOffset.Now.AddDays(-3), Domain.Enums.TokenOriginType.Plan);
            var token2 = new SearchFirmToken(_searchFirmId, DateTimeOffset.Now.AddDays(-1), Domain.Enums.TokenOriginType.Plan);
            var token5 = new SearchFirmToken(_searchFirmId, DateTimeOffset.Now.AddDays(3), Domain.Enums.TokenOriginType.Plan);
            token5.Spend(Guid.NewGuid());

            var validInFuture = new SearchFirmToken(_searchFirmId, DateTimeOffset.UtcNow.AddDays(2).DateTime.Date, Domain.Enums.TokenOriginType.Plan)
                                {
                                    ValidFrom = DateTimeOffset.UtcNow.AddDays(1).UtcDateTime.Date
                                };

            m_FakeCosmos = new FakeCosmos()
                          .EnableContainerFetch(FakeCosmos.SearchFirmsContainerName, _searchFirmId.ToString(), _searchFirmId.ToString(), () => m_SearchFirm)
                          .EnableContainerLinqQuery(FakeCosmos.SearchFirmsContainerName, _searchFirmId.ToString(), () => new[] { token1, token2, m_TokenPurchase, m_TokenPlan, token5 })
                          .EnableContainerFetch(FakeCosmos.PersonsContainerName, m_PersonMissingResults.Id.ToString(), _searchFirmId.ToString(), () => m_PersonMissingResults)
                          .EnableContainerFetch(FakeCosmos.PersonsContainerName, m_PersonInvalidEmailResults.Id.ToString(), _searchFirmId.ToString(), () => m_PersonInvalidEmailResults)
                          .EnableContainerFetch(FakeCosmos.PersonsContainerName, m_Person.Id.ToString(), _searchFirmId.ToString(), () => m_Person)
                          .EnableContainerFetch(FakeCosmos.PersonsContainerName, m_PersonException.Id.ToString(), _searchFirmId.ToString(), () => m_PersonException)
                          .EnableContainerFetch(FakeCosmos.PersonsContainerName, m_PersonWithoutLinkedInUrl.Id.ToString(), _searchFirmId.ToString(), () => m_PersonWithoutLinkedInUrl)
                          .EnableContainerFetch(FakeCosmos.PersonsContainerName, m_PersonPreviouslyFetchedEmails.Id.ToString(), _searchFirmId.ToString(), () => m_PersonPreviouslyFetchedEmails)
                          .EnableContainerReplace<Person>(FakeCosmos.PersonsContainerName, m_PersonWithoutLinkedInUrl.Id.ToString(), _searchFirmId.ToString())
                          .EnableContainerReplace<Person>(FakeCosmos.PersonsContainerName, m_PersonWithoutLinkedInUrl.Id.ToString(), _searchFirmId.ToString())
                          .EnableContainerReplace<Person>(FakeCosmos.PersonsContainerName, m_Person.Id.ToString(), _searchFirmId.ToString())
                          .EnableContainerReplace<Person>(FakeCosmos.PersonsContainerName, m_PersonMissingResults.Id.ToString(), _searchFirmId.ToString())
                          .EnableContainerReplace<SearchFirm>(FakeCosmos.SearchFirmsContainerName, _searchFirmId.ToString(), _searchFirmId.ToString())
                          .EnableContainerReplace<SearchFirmToken>(FakeCosmos.SearchFirmsContainerName, m_TokenPlan.Id.ToString(), _searchFirmId.ToString())
                          .EnableContainerReplace<SearchFirmToken>(FakeCosmos.SearchFirmsContainerName, m_TokenPurchase.Id.ToString(), _searchFirmId.ToString())
                ;

            _fakeRepository.AddToRepository(m_SearchFirm, token1, token2, m_TokenPurchase, m_TokenPlan, token5, m_PersonMissingResults, m_Person, m_PersonException,
                                            m_PersonWithoutLinkedInUrl, m_PersonPreviouslyFetchedEmails, validInFuture);

            m_Query = new Get.Query
            {
                PersonId = m_Person.Id
            };
        }

        [Theory, CombinatorialData]
        public async Task GetSpendsToken(bool isPlan)
        {
            // Given
            SearchFirmToken token;
            if (isPlan)
            {
                m_TokenPurchase.Spend(Guid.NewGuid());
                token = m_TokenPlan;
            }
            else
            {
                m_TokenPlan.Spend(Guid.NewGuid());
                token = m_TokenPurchase;
            }

            var controller = CreateController();
            m_Query = new Get.Query()
            {
                PersonId = m_Person.Id
            };

            // When
            await controller.Get(m_Query);

            // Then
            var updatedPurchaseToken = await _fakeRepository.GetItem<SearchFirmToken>(m_TokenPurchase.SearchFirmId.ToString(), m_TokenPurchase.Id.ToString());
            Assert.True( updatedPurchaseToken.IsSpent);

            var updatedPlanToken = await _fakeRepository.GetItem<SearchFirmToken>(m_TokenPlan.SearchFirmId.ToString(), m_TokenPlan.Id.ToString());
            Assert.True(updatedPlanToken.IsSpent);

            var subscription = await _fakeRepository.GetByQuery<SearchFirmToken, SearchFirmToken>
                                   (_searchFirmId.ToString(),
                                    i => i.Where(s => ValidateToken(s, token.Id, token.OriginType)));

            Assert.NotNull(subscription);
        }

        private bool ValidateToken(SearchFirmToken tokenToValidate, Guid tokenId, TokenOriginType tokenOriginType)
        {
            return tokenToValidate.Id == tokenId &&
                tokenToValidate.OriginType == tokenOriginType &&
                tokenToValidate.SearchFirmId == _searchFirmId &&
                tokenToValidate.IsSpent &&
                tokenToValidate.SpentAt?.Date == DateTimeOffset.UtcNow.Date &&
                tokenToValidate.SpentByUserId == m_SearchFirmUserId;
        }

        [Fact]
        public async Task GetDoesNotSpendTokenWhenNoInfo()
        {
            // Given
            var controller = CreateController();
            m_Query = new Get.Query { PersonId = m_PersonMissingResults.Id };

            // When
            await controller.Get(m_Query);

            // Then
            Assert.False(m_TokenPurchase.IsSpent);
            Assert.False(m_TokenPlan.IsSpent);
        }

        [Fact]
        public async Task GetDoesNotSpendTokenWhenNoValidEmailsAndPhoneNumbers()
        {
            // Given
            var controller = CreateController();
            m_Query = new Get.Query { PersonId = m_PersonInvalidEmailResults.Id };

            // When
            var response = await controller.Get(m_Query);
            var result = (Get.Result)((OkObjectResult)response).Value;

            // Then
            Assert.False(m_TokenPurchase.IsSpent);
            Assert.False(m_TokenPlan.IsSpent);
            Assert.False(m_TokenPlan.IsSpent);
            Assert.Null(result.PhoneNumbers);
            Assert.Null(result.TaggedEmails);
        }

        [Fact]
        public async Task GetContactEmailsAndPhoneNumbersFromRocketReachByLinkedinProfileUrl()
        {
            // Given
            var controller = CreateController();
            m_Query = new Get.Query()
            {
                PersonId = m_Person.Id
            };

            // When
            var actionResult = await controller.Get(m_Query);

            // Then
            var result = (Get.Result)((OkObjectResult)actionResult).Value;

            Assert.Contains(result.TaggedEmails, x => x.Email == m_LookupProfileResponseModel.emails[0].email);
            Assert.Contains(result.TaggedEmails, x => x.Email == m_LookupProfileResponseModel.emails[1].email);
            Assert.Contains(result.TaggedEmails, x => x.Email == m_LookupProfileResponseModel.emails[2].email);
            Assert.DoesNotContain(result.TaggedEmails, x => x.Email == m_LookupProfileResponseModel.emails[3].email);
            Assert.DoesNotContain(result.TaggedEmails, x => x.Email == m_LookupProfileResponseModel.emails[4].email);

            Assert.Contains(result.PhoneNumbers, x => x == m_LookupProfileResponseModel.phones[0].number);
            Assert.Contains(result.PhoneNumbers, x => x == m_LookupProfileResponseModel.phones[1].number);
            Assert.Contains(result.PhoneNumbers, x => x == m_LookupProfileResponseModel.phones[2].number);

            m_RocketReachApi.Verify(rr => rr.LookupProfile(It.Is<string>(a => a == _ROCKET_REACH_API_KEY),
                                                           It.Is<string>(a => a == m_Person.LinkedInProfileUrl)));
        }

        [Fact]
        public async Task GetContactEmailsOrPhoneNumbersNotFoundFromRocketReach()
        {
            // Given
            var controller = CreateController();
            m_Query = new Get.Query()
            {
                PersonId = m_PersonMissingResults.Id
            };

            // When
            var actionResult = await controller.Get(m_Query);

            // Then
            var result = (Get.Result)((OkObjectResult)actionResult).Value;
            Assert.IsType<Get.Result>(result);
            Assert.Empty(result.TaggedEmails);
            Assert.Empty(result.PhoneNumbers);

            m_RocketReachApi.Verify(rr => rr.LookupProfile(It.Is<string>(a => a == _ROCKET_REACH_API_KEY),
                                                           It.Is<string>(a => a == m_PersonMissingResults.LinkedInProfileUrl)), Times.Once);
        }

        [Fact]
        public async Task GetContactEmailsAndPhoneNumbersExceptionFromRocketReach()
        {
            // Given
            var controller = CreateController();
            m_Query = new Get.Query()
            {
                PersonId = m_PersonException.Id
            };

            // When
            var ex = await Record.ExceptionAsync(() => controller.Get(m_Query));

            // Then
            Assert.NotNull(ex);
            Assert.IsType<ResourceNotFoundException>(ex);
        }

        [Fact]
        public async Task GetContactEmailsAndPhoneNumbersFromRocketReachSetInContainer()
        {
            // Given
            var controller = CreateController();
            m_Query = new Get.Query()
            {
                PersonId = m_Person.Id
            };

            // When
            var actionResult = await controller.Get(m_Query);

            // Then
            var result = (Get.Result)((OkObjectResult)actionResult).Value;
            Assert.Equal(3, result.TaggedEmails.Length);
            AssertEmail(m_LookupProfileResponseModel.emails[0], result.TaggedEmails);
            AssertEmail(m_LookupProfileResponseModel.emails[1], result.TaggedEmails);
            AssertEmail(m_LookupProfileResponseModel.emails[2], result.TaggedEmails);

            m_RocketReachApi.Verify(rr => rr.LookupProfile(It.Is<string>(a => a == _ROCKET_REACH_API_KEY),
                                                           It.Is<string>(a => a == m_Person.LinkedInProfileUrl)));

            m_FakeCosmos.PersonsContainer.Verify(c => c.ReplaceItemAsync(It.Is<Person>(p => p.RocketReachFetchedInformation
                                                                                            && p.PhoneNumbers.Contains(m_LookupProfileResponseModel.phones[0].number)
                                                                                            && p.PhoneNumbers.Contains(m_LookupProfileResponseModel.phones[1].number)
                                                                                            && p.PhoneNumbers.Contains(m_LookupProfileResponseModel.phones[2].number)
                                                                                            && p.TaggedEmails.Count == 3
                                                                                            && AssertEmail(m_LookupProfileResponseModel.emails[0], p.TaggedEmails)
                                                                                            && AssertEmail(m_LookupProfileResponseModel.emails[1], p.TaggedEmails)
                                                                                            && AssertEmail(m_LookupProfileResponseModel.emails[2], p.TaggedEmails)),
                                                                         It.Is<string>(a => a == m_Person.Id.ToString()),
                                                                         It.Is<PartitionKey>(p => p == new PartitionKey(m_SearchFirm.Id.ToString())),
                                                                         It.IsAny<ItemRequestOptions>(),
                                                                         It.IsAny<CancellationToken>()));

            m_FakeCosmos.SearchFirmsContainer.Verify(p => p.ReplaceItemAsync(It.IsAny<SearchFirm>(), It.IsAny<string>(), It.IsAny<PartitionKey>(),
                                                                             It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetContactEmailsAndPhoneNumbersNoSearchFirmCredits()
        {
            // Given
            var controller = CreateController();
            m_TokenPlan.Spend(Guid.NewGuid());
            m_TokenPurchase.Spend(Guid.NewGuid());
            m_Query = new Get.Query()
            {
                PersonId = m_Person.Id
            };

            // When
            var actionResult = await controller.Get(m_Query);

            // Then
            var result = (Get.Result)((OkObjectResult)actionResult).Value;
            Assert.IsType<Get.Result>(result);
            Assert.Null(result.TaggedEmails);
            Assert.True(result.CreditsExpired);

            m_FakeCosmos.SearchFirmsContainer.Verify(p => p.ReplaceItemAsync(It.Is<SearchFirm>(s => s.Id == _searchFirmId &&
                                                                                                    s.RocketReachAttemptUseExpiredCredits[0].Date == DateTimeOffset.Now.Date),
                                                                             It.Is<string>(a => a == _searchFirmId.ToString()),
                                                                             It.Is<PartitionKey>(p => p == new PartitionKey(_searchFirmId.ToString())),
                                                                             It.IsAny<ItemRequestOptions>(),
                                                                             It.IsAny<CancellationToken>()));

            m_RocketReachApi.Verify(rr => rr.LookupProfile(It.Is<string>(a => a == _ROCKET_REACH_API_KEY),
                                                           It.Is<string>(a => a == m_Person.LinkedInProfileUrl)), Times.Never);
        }


        [Fact]
        public async Task GetContactEmailsAndPhoneNumbersPreviouslyFetchedEmails()
        {
            // Given
            var controller = CreateController();
            m_TokenPlan.Spend(Guid.NewGuid());
            m_Query = new Get.Query()
            {
                PersonId = m_PersonPreviouslyFetchedEmails.Id
            };

            // When
            var actionResult = await controller.Get(m_Query);

            // Then
            var result = (Get.Result)((OkObjectResult)actionResult).Value;
            Assert.IsType<Get.Result>(result);
            Assert.Null(result.TaggedEmails);
            Assert.Null(result.PhoneNumbers);
            Assert.True(result.RocketReachPreviouslyFetchedEmails);
            m_RocketReachApi.Verify(rr => rr.LookupProfile(It.Is<string>(a => a == _ROCKET_REACH_API_KEY),
                                                           It.Is<string>(a => a == m_PersonPreviouslyFetchedEmails.LinkedInProfileUrl)), Times.Never);
        }


        [Fact]
        public async Task GetOnlyValidDomainsFromRocketReach()
        {
            // Given
            var controller = CreateController();
            m_Query = new Get.Query()
            {
                PersonId = m_Person.Id
            };

            // When
            var actionResult = await controller.Get(m_Query);

            // Then
            var result = (Get.Result)((OkObjectResult)actionResult).Value;
            Assert.Equal(3, result.TaggedEmails.Length);
            AssertEmail(m_LookupProfileResponseModel.emails[0], result.TaggedEmails);
            AssertEmail(m_LookupProfileResponseModel.emails[1], result.TaggedEmails);
            AssertEmail(m_LookupProfileResponseModel.emails[2], result.TaggedEmails);
            m_RocketReachApi.Verify(rr => rr.LookupProfile(It.Is<string>(a => a == _ROCKET_REACH_API_KEY),
                                                           It.Is<string>(a => a == m_Person.LinkedInProfileUrl)));
        }

        [Fact]
        public async Task GetRetriesPersonByUrlWhenRocketReachNotReadyYet()
        {
            // Given
            const int id = 123;
            m_LookupProfileResponseModel.id = id;
            m_RocketReachApi.Setup(x => x.LookupProfile(It.Is<string>(a => a == _ROCKET_REACH_API_KEY),
                                                        It.Is<string>(x => x == m_Person.LinkedInProfileUrl)))
                            .ReturnsAsync(new LookupProfileResponseModel { id = id, status = "searching" });
            m_RocketReachApi.SetupSequence(api => api.CheckStatus(_ROCKET_REACH_API_KEY, id))
                            .ReturnsAsync(new[] { new LookupProfileResponseModel { id = 777, status = "complete" }, new LookupProfileResponseModel { id = id, status = "searching" } })
                            .ReturnsAsync(new[] { m_LookupProfileResponseModel });
            var controller = CreateController();

            // When
            await controller.Get(m_Query);

            // Then
            m_RocketReachApi.Verify(x => x.CheckStatus(It.IsAny<string>(), It.IsAny<int>()), Times.Exactly(2));
        }

        [Fact]
        public async Task GetPersonRetriesNoMoreThanSpecifiedInSettings()
        {
            // Given
            const int byUrlId = 789;
            const int byNameId = 101112;

            m_RocketReachApi.Setup(api => api.LookupProfile(It.Is<string>(key => key == _ROCKET_REACH_API_KEY),
                                                            It.Is<string>(url => url == m_Person.LinkedInProfileUrl)))
                            .ReturnsAsync(new LookupProfileResponseModel { id = byUrlId, status = "searching" });
            m_RocketReachApi.Setup(api => api.CheckStatus(_ROCKET_REACH_API_KEY, byUrlId))
                            .ReturnsAsync(new[] { new LookupProfileResponseModel { id = 777, status = "complete" }, new LookupProfileResponseModel { id = byUrlId, status = "searching" } });

            m_RocketReachApi.Setup(api => api.CheckStatus(_ROCKET_REACH_API_KEY, byNameId))
                            .ReturnsAsync(new[] { new LookupProfileResponseModel { id = 777, status = "complete" }, new LookupProfileResponseModel { id = byNameId, status = "searching" } });

            var controller = CreateController();

            // When
            await controller.Get(m_Query);

            // Then
            m_RocketReachApi.Verify(x => x.CheckStatus(It.IsAny<string>(), byUrlId), Times.Exactly(_rocketReachSettings.RetryNumber - 1));
        }

        private static void AssertEmail(LookupProfileResponseEmailModel expected, Get.Result.TaggedEmail[] resultEmails)
            => Assert.Contains(resultEmails, e => e.Email == expected.email && e.SmtpValid == expected.smtp_valid);

        private static bool AssertEmail(LookupProfileResponseEmailModel expected, List<TaggedEmail> resultEmails)
        {
            Assert.Contains(resultEmails, e => e.Email == expected.email && e.SmtpValid == expected.smtp_valid);
            return true;
        }

        private ContactLookupDetailsController CreateController()
        {
            return new ControllerBuilder<ContactLookupDetailsController>()
                  .AddTransient(m_RocketReachApi.Object)
                  .AddTransient(Options.Create(_rocketReachSettings))
                  .SetFakeCosmos(m_FakeCosmos)
                  .SetSearchFirmUser(_searchFirmId, m_SearchFirmUserId)
                  .SetFakeRepository(_fakeRepository)
                  .Build();
        }

    }
}

