using Ikiru.Parsnips.Api.Controllers.Sectors;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Sector = Ikiru.Parsnips.Api.Controllers.Sectors.GetList.Result.Sector;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Sectors
{
    public class GetListTests
    {
        public static IEnumerable<object[]> SectorsTypeAheadTestData()
            => new[]
               {
                   new object[] { "culture", new [] { new Sector { SectorId = "I1061", Name = "Agriculture" }, new Sector { SectorId = "I146", Name = "Fishing and Aquaculture" } } },
                   new object[] { "animal", new [] { new Sector { SectorId = "I10621", Name = "Animal Feed Manufacturing" }, new Sector { SectorId = "I137", Name = "Animal Health" }, new Sector { SectorId = "I1371", Name = "Animal Pharmaceuticals" } } },
                   new object[] { "ROB", new [] { new Sector { SectorId = "I1633", Name = "Robotics" } } }
               };

        [Theory]
        [MemberData(nameof(SectorsTypeAheadTestData))]
        public async Task GetListReturnsCorrectResultsInOrder(string searchQuery, Sector[] expectedResult)
        {
            // Given
            var controller = CreateController();

            // When
            var actionResult = await controller.GetList(new GetList.Query { Search = searchQuery });

            // Then
            var result = (GetList.Result)((OkObjectResult)actionResult).Value;

            Assert.Equal(expectedResult.Length, result.Sectors.Length);
            for (var i = 0; i < expectedResult.Length; ++i)
            {
                Assert.Equal(expectedResult[i].SectorId, result.Sectors[i].SectorId);
                Assert.Equal(expectedResult[i].Name, result.Sectors[i].Name);
            }
        }

        private static SectorsController CreateController()
        {
            return new ControllerBuilder<SectorsController>()
               .Build();
        }
    }
}
