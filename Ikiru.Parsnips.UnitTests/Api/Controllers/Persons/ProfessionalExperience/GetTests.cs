using Ikiru.Parsnips.Api.Controllers.Persons.ProfessionalExperience;
using Ikiru.Parsnips.Api.ModelBinding;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.UnitTests.Helpers;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Persons.ProfessionalExperience
{
    public class GetTests
    {
        private readonly Guid m_SearchFirmId = Guid.NewGuid();

        private readonly Get.Query m_Query = new Get.Query();
        private readonly Person m_Person;
        
        private readonly FakeCosmos m_FakeCosmos = new FakeCosmos();
        
        public GetTests()
        {
            m_Person = new Person(m_SearchFirmId)
                       {
                           SectorsIds = new List<string>
                                        {
                                            "I12531",
                                            "I1021"
                                        },
                           Keywords = new List<string>
                                        {
                                            "K1",
                                            "K2"
                                        }
            };

            m_FakeCosmos.EnableContainerLinqQuery(FakeCosmos.PersonsContainerName, m_SearchFirmId.ToString(), () => new List<Person> { m_Person });
        }

        [Fact]
        public async Task GetReturnsCorrectResult()
        {
            // Given
            var controller = CreateController();

            // When
            var actionResult = await controller.Get(m_Person.Id, m_Query);

            // Then
            var result = (Get.Result)((OkObjectResult)actionResult).Value;
            Assert.NotNull(result.Sectors);
            Assert.Equal(m_Person.SectorsIds.Count, result.Sectors.Count);
            var firstSector = result.Sectors[0];
            Assert.Equal(m_Person.SectorsIds[0], firstSector.SectorId);
            Assert.Null(firstSector.LinkSector);
            
            var secondSector = result.Sectors[1];
            Assert.Equal(m_Person.SectorsIds[1], secondSector.SectorId);
            Assert.Null(secondSector.LinkSector);


            Assert.Equal(m_Person.Keywords[0], result.Keywords[0]);
            Assert.Equal(m_Person.Keywords[1], result.Keywords[1]);
        }
        
        [Theory]
        [InlineData(new[] { Get.Query.ExpandValue.Sector })]
        public async Task GetListReturnsCorrectExpandResults(Get.Query.ExpandValue[] expand)
        {
            // Given
            var controller = CreateController();
            m_Query.Expand = new ExpandList<Get.Query.ExpandValue>(expand);

            // When
            var actionResult = await controller.Get(m_Person.Id, m_Query);

            // Then
            var result = (Get.Result)((OkObjectResult)actionResult).Value;
            Assert.NotNull(result.Sectors);
            Assert.Equal(m_Person.SectorsIds.Count, result.Sectors.Count);

            var firstSector = result.Sectors[0];
            var secondSector = result.Sectors[1];

            if (expand.Contains(Get.Query.ExpandValue.Sector))
            {
                Assert.NotNull(firstSector.LinkSector);
                Assert.Equal(m_Person.SectorsIds[0], firstSector.LinkSector.SectorId);
                Assert.Equal("Construction of Roads and Railways, bridges and tunnels", firstSector.LinkSector.Name);
                
                Assert.NotNull(secondSector.LinkSector);
                Assert.Equal(m_Person.SectorsIds[1], secondSector.SectorId);
                Assert.Equal("Brewers", secondSector.LinkSector.Name);
            }
        }

        [Fact]
        public async Task GetThrowsWhenPersonNotMatched()
        {
            // Given
            var personId = Guid.NewGuid();
            var controller = CreateController();

            // When
            var ex = await Record.ExceptionAsync(() => controller.Get(personId, m_Query));

            // Then
            ex.AssertNotFoundFailure($"Unable to find 'Person' with Id '{personId}'");
        }

        private ProfessionalExperienceController CreateController()
        {
            return new ControllerBuilder<ProfessionalExperienceController>()
                  .SetFakeCosmos(m_FakeCosmos)
                  .SetSearchFirmUser(m_SearchFirmId)
                  .SetFakeRepository(new FakeRepository())
                  .Build();
        }
    }
}
