using Ikiru.Parsnips.Application.Infrastructure.DataPool.Models.Person;
using Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.IntegrationTests.Helpers.External
{
    public static class FakeDatapoolApi
    {
        public static Guid PersonId = new Guid("be450f00-abcd-1234-aabb-0a1234bc567d");

        public static string PhotoUrl = "https://photo.example.uk/odIStOrPREASwElE";
        public static List<Shared.Infrastructure.DataPoolApi.Models.Person.Person> StubData;
        public static List<Ikiru.Parsnips.Application.Infrastructure.DataPool.Models.Person.Person> RefactorStubData;

        private static List<Shared.Infrastructure.DataPoolApi.Models.Person.Person> SetupData(Guid dataPoolId)
        {
            return new List<Shared.Infrastructure.DataPoolApi.Models.Person.Person>
            {
                new Shared.Infrastructure.DataPoolApi.Models.Person.Person{

                Id = dataPoolId,
                CurrentEmployment = new Shared.Infrastructure.DataPoolApi.Models.Person.Job
                {
                    CompanyId = new Guid("c157ab1f-ac1b-4f15-a26d-0e3603ab993c"),
                    CompanyName = "Ikiru People Limited"
                },
                Location = new Shared.Infrastructure.DataPoolApi.Models.Common.Address
                {
                    MunicipalitySubdivision = "Main address line",
                    Municipality = "gdfsgdfsgdfs",
                    Country = "fgsdfgdfs"
                },
                PersonDetails = new Shared.Infrastructure.DataPoolApi.Models.Person.PersonDetails
                {
                    Biography = "Biography",
                    Name = "John smith",
                    PhotoUrl = "url/of/photo"
                },
                WebsiteLinks = new List<Shared.Infrastructure.DataPoolApi.Models.Common.WebLink>
                    {
                        new Shared.Infrastructure.DataPoolApi.Models.Common.WebLink
                        {
                            LinkTo = Shared.Infrastructure.DataPoolApi.Models.Common.Linkage.Facebook,
                            Url = "https://facebook.com/johnsmith1"
                        },
                        new Shared.Infrastructure.DataPoolApi.Models.Common.WebLink
                        {
                            LinkTo = Shared.Infrastructure.DataPoolApi.Models.Common.Linkage.LinkedInProfile,
                            Url = "https://www.linkedin.com/in/johnsmith"
                        }
                   }
                }
            };
        }

        private static List<Ikiru.Parsnips.Application.Infrastructure.DataPool.Models.Person.Person> RefactorSetupData(Guid dataPoolId)
        {
            return new List<Ikiru.Parsnips.Application.Infrastructure.DataPool.Models.Person.Person>
            {
                new Ikiru.Parsnips.Application.Infrastructure.DataPool.Models.Person.Person{

                Id = dataPoolId,
                CurrentEmployment = new Ikiru.Parsnips.Application.Infrastructure.DataPool.Models.Person.Job
                {
                    CompanyId = new Guid("c157ab1f-ac1b-4f15-a26d-0e3603ab993c"),
                    CompanyName = "Ikiru People Limited",
                    Position = "test person"
                },
                Location = new Ikiru.Parsnips.Application.Infrastructure.DataPool.Models.Common.Address
                {
                    Municipality = "Municipality",
                    CountrySubdivisionName = "CountrySubdivisionName",
                    CountrySecondarySubdivision = "CountrySecondarySubdivision",
                    Country = "Country"
                },
                PersonDetails = new Ikiru.Parsnips.Application.Infrastructure.DataPool.Models.Person.PersonDetails
                {
                    Biography = "Biography",
                    Name = "John smith",
                    PhotoUrl = "url/of/photo"
                },
                WebsiteLinks = new List<Ikiru.Parsnips.Application.Infrastructure.DataPool.Models.Common.WebLink>
                    {
                        new Ikiru.Parsnips.Application.Infrastructure.DataPool.Models.Common.WebLink
                        {
                            LinkTo = Ikiru.Parsnips.Application.Infrastructure.DataPool.Models.Common.Linkage.Facebook,
                            Url = "https://facebook.com/johnsmith1"
                        },
                        new Ikiru.Parsnips.Application.Infrastructure.DataPool.Models.Common.WebLink
                        {
                            LinkTo = Ikiru.Parsnips.Application.Infrastructure.DataPool.Models.Common.Linkage.LinkedInProfile,
                            Url = "https://www.linkedin.com/in/johnsmith"
                        }
                   }
                }
            };
        }

        public static Mock<IDataPoolApi> Setup(Guid dataPoolId)
        {
            StubData = SetupData(dataPoolId);

            var datapoolApiMock = new Mock<IDataPoolApi>();

            datapoolApiMock.Setup(x => x.GetByWebsiteUrl(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                         .Returns(Task.FromResult(StubData));

            datapoolApiMock
               .Setup(api => api.GetPersonPhotoUrl(It.Is<Guid>(id => id == PersonId), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Shared.Infrastructure.DataPoolApi.Models.Person.PersonPhoto { Photo = new Shared.Infrastructure.DataPoolApi.Models.Person.PersonPhoto.PersonPhotoUrl { Url = PhotoUrl } });

            datapoolApiMock
              .Setup(api => api.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .Returns(Task.FromResult(StubData.First()));

            datapoolApiMock
               .Setup(s => s.GetSimilarPersons(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
               .Returns(Task.FromResult(new List<Shared.Infrastructure.DataPoolApi.Models.Person.Person>()));
            return datapoolApiMock;

        }

        public static Mock<Ikiru.Parsnips.Infrastructure.Datapool.IDataPoolAPI> SetupRefactor(Guid dataPoolId)
        {
            RefactorStubData = RefactorSetupData(dataPoolId);

            var datapoolApiMock = new Mock<Ikiru.Parsnips.Infrastructure.Datapool.IDataPoolAPI>();

            datapoolApiMock.Setup(x => x.SendPersonScraped(It.IsAny<JsonDocument>()))
                            .Returns(Task.FromResult(RefactorStubData.First()));

            datapoolApiMock.Setup(x => x.GetPeronsByWebsiteUrl(It.IsAny<string>()))
                      .Returns(Task.FromResult(RefactorStubData));


            datapoolApiMock.Setup(api => api.SearchPerson(It.IsAny<string>()))
             .Returns(Task.FromResult(new Parsnips.Infrastructure.Datapool.Models.DataPoolPersonSearchResults<Application.Infrastructure.DataPool.Models.Person.Person>
             {
                 FirstItemOnPage = 4,
                 HasNextPage = true,
                 HasPreviousPage = true,
                 IsFirstPage = false,
                 IsLastPage = false,
                 LastItemOnPage = 6,
                 PageCount = 9,
                 PageSize = 3,
                 PageNumber = 2,
                 TotalItemCount = 36,
                 Results = new List<Application.Infrastructure.DataPool.Models.Person.Person>()
                 {
                    new Application.Infrastructure.DataPool.Models.Person.Person()
                    {
                        Id = new Guid("7533ba5b-30b6-4f23-8de1-7401092f847e"),
                        WebsiteLinks = new List<Application.Infrastructure.DataPool.Models.Common.WebLink>
                        {
                            new Application.Infrastructure.DataPool.Models.Common.WebLink
                            {
                                Id = Guid.NewGuid(),
                                LinkTo = Application.Infrastructure.DataPool.Models.Common.Linkage.BloombergProfile,
                                Url = "https://bloomberg.com/test123"
                            }
                        },
                        PersonDetails = new Application.Infrastructure.DataPool.Models.Person.PersonDetails
                        {
                            Name = "Test Person 1",
                            PhotoUrl = "https://myphoto.com"
                        },
                        CurrentEmployment = new Application.Infrastructure.DataPool.Models.Person.Job
                        {
                            CompanyName = "Company 1",
                            StartDate = DateTimeOffset.Now.AddMonths(-3),
                            EndDate = null,
                            Position = "my role 1"
                        },
                        PreviousEmployment = new List<Application.Infrastructure.DataPool.Models.Person.Job>
                        {
                            new Application.Infrastructure.DataPool.Models.Person.Job
                            {
                                CompanyName = "previous company 1",
                                StartDate = DateTimeOffset.Now.AddMonths(-12),
                                EndDate = DateTimeOffset.Now.AddMonths(-3),
                                Position = "my role 2"
                            },
                            new Application.Infrastructure.DataPool.Models.Person.Job
                            {
                                CompanyName = "previous company 2",
                                StartDate = DateTimeOffset.Now.AddMonths(-24),
                                EndDate = DateTimeOffset.Now.AddMonths(-12),
                                Position = "my role 3"
                            }
                        },
                        Location = new Application.Infrastructure.DataPool.Models.Common.Address
                        {
                            Municipality = "Municipality",
                            CountrySubdivisionName = "CountrySubdivisionName",
                            CountrySecondarySubdivision = "CountrySecondarySubdivision",
                            Country = "Country"
                        }
                 }

             }
             }));

            return datapoolApiMock;
        }

    }
}
