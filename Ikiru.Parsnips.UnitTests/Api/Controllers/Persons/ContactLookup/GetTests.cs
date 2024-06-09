using Ikiru.Parsnips.Api.Controllers.Persons.ContactLookup;
using Ikiru.Parsnips.Api.RocketReach;
using Ikiru.Parsnips.Api.RocketReach.Models;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ResourceNotFound;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure;
using Ikiru.Parsnips.UnitTests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Persons.ContactLookup
{
    public class GetTests
    {
        private readonly Mock<IRocketReachApi> m_RocketReachApi;
        private readonly string m_linkedinProfileUrl = "https://www.linkedin.com/in/voyager-training-219196134/";
        private readonly IList<string> m_TeaserEmailsByLinkedin;
        private readonly IList<SearchResponseModel.Phone> m_TeaserPhoneNumbersByLinkedin;
        private readonly IList<string> m_TeaserEmailsByNameAndCompany;
        private readonly IList<SearchResponseModel.Phone> m_TeaserPhoneNumbersByNameAndCompany;
        private readonly FakeCosmos m_FakeCosmos;
        private readonly Person m_Person;
        private readonly Person m_PersonWithoutLinkedInUrl;
        private readonly Person m_PersonEmailTeaserInPreviewSectionInJson;
        private readonly Person m_PersonMissingResults;
        private readonly Person m_PersonException;
        private readonly Person m_PersonPreviousFetchedEmails;
        private readonly SearchFirm m_SearchFirm;
        private const string _ROCKET_REACH_API_KEY = "testkey";
        private readonly Person m_PersonNoCompanyName;
        private readonly Person m_PersonNoPersonName;
        private Get.Query m_Query;
        private readonly FakeRepository _fakeRepository = new FakeRepository();

        public GetTests()
        {
            m_SearchFirm = new SearchFirm
            {
                Name = "Test Company Name"
            };

            m_PersonMissingResults = new Person(m_SearchFirm.Id, linkedInProfileUrl: "https://linkedin.com/in/noresults")
            {
                Organisation = "no result test org 1",
                Name = "Person Name no results"
            };

            m_PersonWithoutLinkedInUrl = new Person(m_SearchFirm.Id)
            {
                Organisation = "no linkedin urltest org 1",
                Name = "Person Name no linkedin url"
            };

            m_PersonEmailTeaserInPreviewSectionInJson = new Person(m_SearchFirm.Id, linkedInProfileUrl: "https://linkedin.com/in/previewdata")
            {
                Organisation = "preview data test org 1",
                Name = "Person preview data"
            };


            m_PersonNoCompanyName = new Person(m_SearchFirm.SearchFirmId)
            {
                Name = "Person Name"
            };

            m_PersonNoPersonName = new Person(m_SearchFirm.SearchFirmId)
            {
                Organisation = "test org"
            };

            m_Person = new Person(m_SearchFirm.SearchFirmId, linkedInProfileUrl: m_linkedinProfileUrl)
            {
                Organisation = "test org 1",
                Name = "Person Name"
            };


            m_PersonException = new Person(m_SearchFirm.SearchFirmId, linkedInProfileUrl: "https://linkedin.com/in/exception")
            {
                Organisation = "exception test org 1",
                Name = "Person exception"
            };

            m_PersonPreviousFetchedEmails = new Person(m_SearchFirm.SearchFirmId)
            {
                Organisation = "FetchedEmails test org 1",
                Name = "Person FetchedEmails",
                RocketReachFetchedInformation = true
            };

            m_TeaserPhoneNumbersByNameAndCompany = new SearchResponseModel.Phone[]
            {
                new SearchResponseModel.Phone
                {
                    number = "000-111-xxx",
                    is_premium = "false"
                },
                 new SearchResponseModel.Phone
                {
                    number = "555-444777xxx",
                    is_premium = "true"
                },
            };

            m_TeaserEmailsByNameAndCompany = new[] { "hotmail1.com", "gmail1.com", "invalidEmail" };
            var searchResponseFromNameAndCompanyModel = new SearchResponseModel
            {
                profiles = new[]
                                                                         {
                                                                             new SearchResponseModel.Profile
                                                                             {
                                                                                 teaser = new SearchResponseModel.Teaser
                                                                                          {
                                                                                              emails = m_TeaserEmailsByNameAndCompany.ToArray(),
                                                                                              phones = m_TeaserPhoneNumbersByNameAndCompany.ToArray()
                                                                                          }
                                                                             }
                                                                         }
            };

            m_TeaserEmailsByLinkedin = new[] { "hotmail.com", "gmail.com", "invalidEmail" };
            m_TeaserPhoneNumbersByLinkedin = new SearchResponseModel.Phone[]
            {
                new SearchResponseModel.Phone
                {
                    number = "111-222-xxx",
                    is_premium = "false"
                },
                 new SearchResponseModel.Phone
                {
                    number = "333-444-xxx",
                    is_premium = "true"
                },
            };

            var searchResponseByLinkedinUrlModel = new SearchResponseModel
            {
                profiles = new[]
                                                                    {
                                                                        new SearchResponseModel.Profile
                                                                        {
                                                                            teaser = new SearchResponseModel.Teaser
                                                                                     {
                                                                                         emails = m_TeaserEmailsByLinkedin.ToArray(),
                                                                                         phones = m_TeaserPhoneNumbersByLinkedin.ToArray()
                                                                                     }
                                                                        }
                                                                    }
            };


            var searchResponseInPreviewByLinkedinUrlModel = new SearchResponseModel
            {
                profiles = new[]
                                                                  {
                                                                      new SearchResponseModel.Profile
                                                                      {
                                                                          teaser = new SearchResponseModel.Teaser
                                                                                   {
                                                                                       preview = m_TeaserEmailsByLinkedin as object[]
                                                                                   }
                                                                      }
                                                                  }
            };

            m_RocketReachApi = new Mock<IRocketReachApi>();

            m_FakeCosmos = new FakeCosmos()
                .EnableContainerFetch(FakeCosmos.PersonsContainerName, m_Person.Id.ToString(), m_SearchFirm.Id.ToString(), () => m_Person)
                .EnableContainerFetch(FakeCosmos.PersonsContainerName, m_PersonNoCompanyName.Id.ToString(), m_SearchFirm.Id.ToString(), () => m_PersonNoCompanyName)
                .EnableContainerFetch(FakeCosmos.PersonsContainerName, m_PersonNoPersonName.Id.ToString(), m_SearchFirm.Id.ToString(), () => m_PersonNoPersonName)
                .EnableContainerFetch(FakeCosmos.PersonsContainerName, m_PersonWithoutLinkedInUrl.Id.ToString(), m_SearchFirm.Id.ToString(), () => m_PersonWithoutLinkedInUrl)
                .EnableContainerFetch(FakeCosmos.PersonsContainerName, m_PersonMissingResults.Id.ToString(), m_SearchFirm.Id.ToString(), () => m_PersonMissingResults)
                .EnableContainerFetch(FakeCosmos.PersonsContainerName, m_PersonException.Id.ToString(), m_SearchFirm.Id.ToString(), () => m_PersonException)
                .EnableContainerFetch(FakeCosmos.PersonsContainerName, m_PersonPreviousFetchedEmails.Id.ToString(), m_SearchFirm.Id.ToString(), () => m_PersonPreviousFetchedEmails)
                .EnableContainerFetch(FakeCosmos.PersonsContainerName, m_PersonEmailTeaserInPreviewSectionInJson.Id.ToString(), m_SearchFirm.Id.ToString(), () => m_PersonEmailTeaserInPreviewSectionInJson)
                .EnableContainerFetch(FakeCosmos.SearchFirmsContainerName, m_SearchFirm.Id.ToString(), m_SearchFirm.Id.ToString(), () => m_SearchFirm);

            _fakeRepository.AddToRepository(m_Person, m_PersonNoCompanyName, m_PersonNoPersonName, m_PersonWithoutLinkedInUrl,
                                            m_PersonMissingResults, m_PersonException, m_PersonPreviousFetchedEmails, m_PersonEmailTeaserInPreviewSectionInJson, m_SearchFirm);

            m_RocketReachApi.Setup(x => x.SearchForPersonDetails(It.IsAny<string>(),
                                                                 It.Is<SearchRequestModel>(searchRequestModel => searchRequestModel.query.keywords.Contains(m_linkedinProfileUrl))))
                            .Returns<string, SearchRequestModel>((a, b) => Task.FromResult(searchResponseByLinkedinUrlModel));

            m_RocketReachApi.Setup(x => x.SearchForPersonDetails(It.IsAny<string>(),
                                                                 It.Is<SearchRequestModel>(searchRequestModel =>
                                                                                               searchRequestModel.query.current_employer.Contains(m_PersonWithoutLinkedInUrl.Organisation) &&
                                                                                                searchRequestModel.query.name.Contains(m_PersonWithoutLinkedInUrl.Name))))
                            .Returns<string, SearchRequestModel>((a, b) => Task.FromResult(searchResponseFromNameAndCompanyModel));

            m_RocketReachApi.Setup(x => x.SearchForPersonDetails(It.IsAny<string>(),
                                                                 It.Is<SearchRequestModel>(searchRequestModel =>
                                                                                               searchRequestModel.query.current_employer.Contains(m_PersonMissingResults.Organisation) &&
                                                                                                searchRequestModel.query.name.Contains(m_PersonMissingResults.Name))))
                            .Returns<string, SearchRequestModel>((a, b) => Task.FromResult(new SearchResponseModel()));

            m_RocketReachApi.Setup(x => x.SearchForPersonDetails(It.IsAny<string>(),
                                                                 It.Is<SearchRequestModel>(searchRequestModel =>
                                                                                               searchRequestModel.query.keywords.Contains(m_PersonEmailTeaserInPreviewSectionInJson.LinkedInProfileUrl))))
                            .Returns<string, SearchRequestModel>((a, b) => Task.FromResult(searchResponseInPreviewByLinkedinUrlModel));

            m_RocketReachApi.Setup(x => x.SearchForPersonDetails(It.IsAny<string>(),
                                                                 It.Is<SearchRequestModel>(searchRequestModel => searchRequestModel.query.keywords.Contains(m_PersonException.LinkedInProfileUrl))))
                            .Throws<Exception>();
        }


        [Fact]
        public async Task GetNotLoadTeasersWhenAlreadyObtained()
        {
            // Given
            var controller = CreateController();

            m_Query = new Get.Query
            {
                PersonId = m_PersonPreviousFetchedEmails.Id
            };

            // When
            var actionResult = await controller.Get(m_Query);

            // Then
            var result = (Get.Result)((OkObjectResult)actionResult).Value;
            Assert.True(result.RocketReachPreviouslyFetchedTeasers);
        }

        [Fact]
        public async Task GetTeasersFromRocketReachByLinkedinProfile()
        {
            // Given
            var controller = CreateController();

            m_Query = new Get.Query
            {
                PersonId = m_Person.Id
            };

            // When
            var actionResult = await controller.Get(m_Query);

            // Then
            var result = (Get.Result)((OkObjectResult)actionResult).Value;
            Assert.IsType<Get.Result>(result);
            Assert.NotEmpty(result.EmailTeasers);
            Assert.NotEmpty(result.PhoneTeasers);
            m_RocketReachApi.Verify(rr => rr.SearchForPersonDetails(It.Is<string>(a => a == _ROCKET_REACH_API_KEY),
                                                                    It.Is<SearchRequestModel>(srm =>
                                                                                                  srm.query.keywords.Contains(m_linkedinProfileUrl)
                                                                                             )));
        }

        [Fact]
        public async Task GetTeaserEmailsFromPreviewSectionFromRocketReach()
        {
            // Given
            var controller = CreateController();
            m_Query = new Get.Query
            {
                PersonId = m_PersonEmailTeaserInPreviewSectionInJson.Id
            };

            // When
            var actionResult = await controller.Get(m_Query);

            // Then
            var result = (Get.Result)((OkObjectResult)actionResult).Value;
            Assert.IsType<Get.Result>(result);
            Assert.Equal(m_TeaserEmailsByLinkedin[0], result.EmailTeasers[0]);
            Assert.Equal(m_TeaserEmailsByLinkedin[1], result.EmailTeasers[1]);

            m_RocketReachApi.Verify(rr => rr.SearchForPersonDetails(It.Is<string>(a => a == _ROCKET_REACH_API_KEY),
                                                                    It.Is<SearchRequestModel>(srm => srm.query.keywords
                                                                                                    .Contains(m_PersonEmailTeaserInPreviewSectionInJson.LinkedInProfileUrl))));

        }

        [Fact]
        public async Task GetTeasersNotFoundFromRocketReach()
        {
            // Given
            var controller = CreateController();
            m_Query = new Get.Query
            {
                PersonId = m_PersonMissingResults.Id
            };

            // When
            var actionResult = await controller.Get(m_Query);

            // Then
            var result = (Get.Result)((OkObjectResult)actionResult).Value;
            Assert.IsType<Get.Result>(result);
            Assert.Empty(result.EmailTeasers);
            Assert.Empty(result.PhoneTeasers);

            m_RocketReachApi.Verify(rr => rr.SearchForPersonDetails(It.Is<string>(a => a == _ROCKET_REACH_API_KEY),
                                                                    It.Is<SearchRequestModel>(srm =>
                                                                                                  srm.query.keywords.Contains(m_PersonMissingResults.LinkedInProfileUrl)
                                                                                             )));
        }

        [Fact]
        public async Task GetOnlyValidDomainsFromRocketReach()
        {
            // Given
            var controller = CreateController();
            m_Query = new Get.Query
            {
                PersonId = m_Person.Id
            };

            // When
            var actionResult = await controller.Get(m_Query);

            // Then
            var result = (Get.Result)((OkObjectResult)actionResult).Value;
            Assert.IsType<Get.Result>(result);
            Assert.Equal(m_TeaserEmailsByLinkedin[0], result.EmailTeasers[0]);
            Assert.Equal(m_TeaserEmailsByLinkedin[1], result.EmailTeasers[1]);
            Assert.Equal(2, result.EmailTeasers.Length); // Check invalid email domain removed.

            m_RocketReachApi.Verify(rr => rr.SearchForPersonDetails(It.Is<string>(a => a == _ROCKET_REACH_API_KEY),
                                                                    It.Is<SearchRequestModel>(srm =>
                                                                                                  srm.query.keywords.Contains(m_Person.LinkedInProfileUrl)
                                                                                             )));
        }

        [Fact]
        public async Task GetTeasersExceptionFromRocketReach()
        {
            // Given
            var controller = CreateController();
            m_Query = new Get.Query
            {
                PersonId = m_PersonException.Id
            };

            // When
            var ex = await Record.ExceptionAsync(() => controller.Get(m_Query));

            // Then
            Assert.NotNull(ex);
            Assert.IsType<ResourceNotFoundException>(ex);
        }
            
        private ContactLookupController CreateController()
        {
            return new ControllerBuilder<ContactLookupController>()
                  .SetFakeCosmos(m_FakeCosmos)
                  .SetSearchFirmUser(m_SearchFirm.SearchFirmId)
                  .AddTransient(m_RocketReachApi.Object)
                  .SetFakeRepository(_fakeRepository)
                  .Build();
        }
    }
}
