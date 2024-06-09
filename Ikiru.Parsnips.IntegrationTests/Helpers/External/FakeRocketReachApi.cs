using System.Linq;
using System.Threading.Tasks;
using Ikiru.Parsnips.Api.RocketReach;
using Ikiru.Parsnips.Api.RocketReach.Models;
using Moq;

namespace Ikiru.Parsnips.IntegrationTests.Helpers.External
{
    public static class FakeRocketReachApi
    {
        public static Mock<IRocketReachApi> RocketReachApi => Setup();

        private static Mock<IRocketReachApi> Setup()
        {
            var teaserEmails = new[] { "hotmail.com", "gmail.com" };
            var personEmails = new[] { "Jim.B@hotmail.com", "jim.bono@gmail.com" };

            var teaserPhoneNumbers = new[] {
                                                new SearchResponseModel.Phone
                                                {
                                                    number = "000-111-xxx"
                                                },
                                                new SearchResponseModel.Phone
                                                {
                                                    number = "222-444-xxx"
                                                }
                                            };


             var personPhoneNumbers = new[] {
                                                new SearchResponseModel.Phone
                                                {
                                                    number = "000-111-222"
                                                },
                                                new SearchResponseModel.Phone
                                                {
                                                    number = "222-444-333"
                                                }
                                            };

            var searchResponse = new SearchResponseModel
            {
                profiles = new[]
                {
                    new SearchResponseModel.Profile
                     {
                         status = "complete",
                         teaser = new SearchResponseModel.Teaser
                                  {
                                      emails = teaserEmails.ToArray(),
                                      phones = teaserPhoneNumbers.ToArray()
                                  }
                     }
                 }
            };

            var lookupProfileResponseModel = new LookupProfileResponseModel
            {
                status = "complete",
                phones = new[]
                {
                    new LookupProfileResponsePhoneNumberModel
                    {
                        number  = personPhoneNumbers[0].number
                    },
                    new LookupProfileResponsePhoneNumberModel
                    {
                        number  = personPhoneNumbers[1].number
                    }
                },
                emails = new[]
                                  {
                                      new LookupProfileResponseEmailModel { email = personEmails[0] },
                                      new LookupProfileResponseEmailModel { email = personEmails[1] }
                                  }
            };

            var rocketReachApi = new Mock<IRocketReachApi>();
            rocketReachApi.Setup(x => x.SearchForPersonDetails(It.IsAny<string>(),
                                                                                        It.IsAny<SearchRequestModel>()))
                              .Returns<string, SearchRequestModel>((a, b) => Task.FromResult(searchResponse));


            rocketReachApi.Setup(x => x.LookupProfile(It.IsAny<string>(),
                                                      It.IsAny<string>()))
                          .Returns<string, string>((a, b) => Task.FromResult(lookupProfileResponseModel));


            rocketReachApi.Setup(x => x.LookupProfile(It.IsAny<string>(),
                                                      It.IsAny<string>(),
                                                      It.IsAny<string>()))
                          .Returns<string, string, string>((a, b, c) => Task.FromResult(lookupProfileResponseModel));

            return rocketReachApi;
        }
    }
}
