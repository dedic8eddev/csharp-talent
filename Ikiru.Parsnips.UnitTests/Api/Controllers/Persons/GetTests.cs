using Azure;
using Ikiru.Parsnips.Api.Controllers.Persons;
using Ikiru.Parsnips.Application.Infrastructure.DataPool;
using Ikiru.Parsnips.Application.Services.DataPool;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ResourceNotFound;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Blobs;
using Ikiru.Parsnips.UnitTests.Helpers;
using Ikiru.Parsnips.UnitTests.Helpers.Validation;
using Ikiru.Persistence.Repository;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using DataPoolApiModel = Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi.Models;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Persons
{
    public class GetTests
    {
        private readonly Guid m_SearchFirmId = Guid.NewGuid();
        private readonly Get.Query m_Query = new Get.Query();

        private readonly FakeCosmos m_FakeCosmos = new FakeCosmos();
        private readonly FakeCloud m_FakeCloud = new FakeCloud();
        private readonly string m_DataPoolPhotoUrl = "https://photo.url/person";
        private readonly Mock<IDataPoolService> m_DataPoolServiceMock;
        private readonly Person m_Person;
        private readonly List<DataPoolApiModel.Person.Person> m_DatapoolPersonsStub;
        private bool m_PhotoExists = true;
        private string PhotoBlobPath => $"{m_SearchFirmId}/{m_Person.Id}/photo";
        private string m_LinkedInUrl = "https://www.linkedin.com/in/some-valid-linked-in-profileurl";
        private string m_DataPoolOnlyLinkedInUrl = "https://www.linkedin.com/in/some-datapoolonly-linked-in-profileurl";

         private readonly Mock<IRepository> m_RepositoryMock = new Mock<IRepository>();
        private readonly Mock<IPersonInfrastructure> m_PersonInfrastructure = new Mock<IPersonInfrastructure>();
        public GetTests()
        {
            m_DataPoolServiceMock = new Mock<IDataPoolService>();

            m_DatapoolPersonsStub = new List<DataPoolApiModel.Person.Person>
            {
                new DataPoolApiModel.Person.Person
                {
                    Id = Guid.NewGuid(),
                    CurrentEmployment = new DataPoolApiModel.Person.Job
                    {
                        CompanyId = new Guid("c157ab1f-ac1b-4f15-a26d-0e3603ab993c"),
                        CompanyName = "Ikiru People Limited"
                    },
                    Location = new DataPoolApiModel.Common.Address
                    {
                        MunicipalitySubdivision = "Main address line",
                        Municipality = "adsfdas",
                        Country = "CountryName"
                    },
                    PersonDetails = new DataPoolApiModel.Person.PersonDetails
                    {
                        Biography = "Biography",
                        Name = "John smith",
                        PhotoUrl = "url/of/photo"
                    },
                    WebsiteLinks = new List<DataPoolApiModel.Common.WebLink>
                    {
                        new DataPoolApiModel.Common.WebLink
                        {
                            LinkTo = DataPoolApiModel.Common.Linkage.Facebook,
                            Url = "https://facebook.com/johnsmith1"
                        },
                        new DataPoolApiModel.Common.WebLink
                        {
                            LinkTo = DataPoolApiModel.Common.Linkage.LinkedInProfile,
                            Url = m_DataPoolOnlyLinkedInUrl                        }
                    }
                }
            };

            m_Person = new Person(m_SearchFirmId, null, m_LinkedInUrl)
            {
                Name = "Jack Barber",
                DataPoolPersonId = m_DatapoolPersonsStub[0].Id,
                JobTitle = "Senior Evangelist WinForms design engineer PHD",
                Location = "Basingstoke, UK",
                TaggedEmails = new List<TaggedEmail> { new TaggedEmail { Email = "jack@fruityparsnips.com" }, new TaggedEmail { Email = "fruit@jackyparsnips.com" } },
                PhoneNumbers = new List<string> { "01234 567890", "00000 333333" },
                Organisation = "Barber&Son",
                GdprLawfulBasisState = new PersonGdprLawfulBasisState
                {
                    GdprLawfulBasisOptionsStatus = GdprLawfulBasisOptionsStatusEnum.ConsentGiven,
                    GdprLawfulBasisOption = GdprLawfulBasisOptionEnum.DigitalConsent,
                    GdprDataOrigin = "there is a connection with the person",
                },
                Keywords = new List<string> { "Lots of sunshine", "Perfect entertainment choice" },
                WebSites = new List<PersonWebsite> { new PersonWebsite { Url = "https://twitter.com/talentis-ceo", Type = WebSiteType.Twitter } }
            };

            m_FakeCosmos.EnableContainerLinqQuery(FakeCosmos.PersonsContainerName, m_SearchFirmId.ToString(), () => new List<Person> { m_Person });

            m_FakeCloud.SeedFor(BlobStorage.ContainerNames.PersonsDocuments, PhotoBlobPath)
                       .Setup(c => c.ExistsAsync(It.IsAny<CancellationToken>()))
                       .Returns<CancellationToken>(_ => Task.FromResult(Mock.Of<Response<bool>>(r => r.Value == m_PhotoExists)));

            m_DataPoolServiceMock.Setup(x => x.GetSinglePersonById(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .Returns(Task.FromResult(m_DatapoolPersonsStub[0]));

            m_DataPoolServiceMock.Setup(x => x.GetTempAccessPhotoUrl(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(m_DataPoolPhotoUrl));
        }

        [Fact]
        public async Task GetById()
        {
            // Given
            var controller = CreateController();
            m_Query.Id = m_Person.Id;

            // When
            var actionResult = await controller.Get(m_Query);

            // Then
            var result = (Get.Result)((OkObjectResult)actionResult).Value;
            Assert.Equal(m_Person.Organisation, result.LocalPerson.Company);
            Assert.Equal(m_Person.JobTitle, result.LocalPerson.JobTitle);
            m_Person.TaggedEmails.AssertSameList(result.LocalPerson.TaggedEmails);
            Assert.Equal(m_Person.LinkedInProfileUrl, result.LocalPerson.LinkedInProfileUrl);
            Assert.Equal(m_Person.Name, result.LocalPerson.Name);
            Assert.Equal(m_Person.Location, result.LocalPerson.Location);
            Assert.Equal(m_Person.PhoneNumbers, result.LocalPerson.PhoneNumbers);
            Assert.Equal(m_DatapoolPersonsStub[0].Id, result.LocalPerson.DataPoolId);
            Assert.Equal(m_Person.GdprLawfulBasisState.GdprLawfulBasisOptionsStatus, result.LocalPerson.GdprLawfulBasisState.GdprLawfulBasisOptionsStatus);
            Assert.Equal(m_Person.GdprLawfulBasisState.GdprLawfulBasisOption, result.LocalPerson.GdprLawfulBasisState.GdprLawfulBasisOption);
            Assert.Equal(m_Person.GdprLawfulBasisState.GdprDataOrigin, result.LocalPerson.GdprLawfulBasisState.GdprDataOrigin);
            Assert.Equal(m_Person.Keywords, result.LocalPerson.Keywords);
            Assert.True(m_Person.WebSites.IsSameList(result.LocalPerson.WebSites, (d, r) => d.Url == r.Url && d.Type == r.Type));

            Assert.StartsWith($"{FakeCloud.BASE_URL}/{BlobStorage.ContainerNames.PersonsDocuments}/{PhotoBlobPath}?", result.LocalPerson.Photo.Url);

            result.LocalPerson.Photo.Url.AssertThatSaSUrl()
                  .HasStartNoOlderThanSeconds(65)
                  .HasEndNoMoreThanMinutesInFuture(10)
                  .HasSignature()
                  .HasPermissionEquals("r");

            Assert.Equal(m_DatapoolPersonsStub[0].Id, result.DataPoolPerson.DataPoolId);
            Assert.Equal(Guid.Empty, result.DataPoolPerson.Id);

            Assert.Equal(m_DatapoolPersonsStub[0].PersonDetails.Name, result.DataPoolPerson.Name);
            Assert.Equal(m_DatapoolPersonsStub[0].CurrentEmployment.CompanyName, result.DataPoolPerson.Company);
            Assert.True(!string.IsNullOrEmpty(result.DataPoolPerson.Location));
            Assert.NotEmpty(result.DataPoolPerson.WebSites);

            m_DataPoolServiceMock.Verify(x => x.GetSinglePersonById(It.Is<string>(p => p == m_Person.DataPoolPersonId.ToString()),
                                                                    It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetReturnsNullForPhotoIfNone()
        {
            // Given
            var controller = CreateController();
            m_Query.Id = m_Person.Id;
            m_PhotoExists = false;

            // When
            var actionResult = await controller.Get(m_Query);

            // Then
            var result = (Get.Result)((OkObjectResult)actionResult).Value;
            Assert.Null(result.LocalPerson.Photo);
        }

        [Fact]
        public async Task GetByIdThrowsWhenNoPerson()
        {
            // Given
            m_Query.Id = Guid.NewGuid();
            var controller = CreateController();

            // When
            var ex = await Record.ExceptionAsync(() => controller.Get(m_Query));

            // Then
            Assert.NotNull(ex);
            Assert.IsType<ResourceNotFoundException>(ex);
        }

        private PersonsController CreateController()
        {
            return new ControllerBuilder<PersonsController>()
                  .SetFakeCosmos(m_FakeCosmos)
                  .SetFakeCloud(m_FakeCloud)
                  .SetSearchFirmUser(m_SearchFirmId)
                    .AddTransient(m_RepositoryMock.Object)
                    .AddTransient(m_PersonInfrastructure.Object)
                  .AddTransient(m_DataPoolServiceMock.Object)
                  .Build();
        }
    }
}
