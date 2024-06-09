using AutoMapper;
using Ikiru.Parsnips.Application.Infrastructure.DataPool.Models;
using Ikiru.Parsnips.Application.Infrastructure.DataPool.Models.Person;
using Ikiru.Parsnips.Infrastructure.Datapool;
using Ikiru.Parsnips.Infrastructure.Datapool.Models;
using Moq;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Infrastructure.Datapool
{
    public class DataPoolPersonTests
    {
        private readonly Mock<IDataPoolAPI> _dataPoolAPIMock = new Mock<IDataPoolAPI>();
        private readonly SearchPersonQueryRequest _query = new SearchPersonQueryRequest();
        private readonly string _bundleKeword1 = "bundle1";
        private readonly string _bundleKeword2 = "bundle2";
        private readonly string _keword1 = "keyword1";
        private readonly string _keword2 = "keyword2";

        public DataPoolPersonTests()
        {
            _dataPoolAPIMock
                .Setup(api => api.SearchPerson(It.IsAny<string>()))
                .ReturnsAsync(new DataPoolPersonSearchResults<Person>());
        }

        [Theory, CombinatorialData]
        public async Task SearchPersonsCallDatapoolWithCorrectKeywords(bool isSearchBundle)
        {
            _query.KeywordBundle = new[] { new KeywordSearch { Keywords = new[] { _bundleKeword1, _bundleKeword2 }, KeywordsSearchUsingORLogic = true } };

            var personInfrastructure = CreateDataPoolPerson();

            // Act
            await personInfrastructure.SearchPersons(_query);

            // Assert
            _dataPoolAPIMock.Verify(d => d.SearchPerson(It.Is<string>(json => ValidatePersonSearchCriteria(json, isSearchBundle))));
        }

        private bool ValidatePersonSearchCriteria(string searchJson, bool isSearchBundle)
        {
            var searchQuery = System.Text.Json.JsonSerializer.Deserialize<SearchPersonQueryRequest>(searchJson);
            var bundle = searchQuery.KeywordBundle.Single();
            Assert.Single(bundle.Keywords.Where(k => k == _bundleKeword1));
            Assert.Single(bundle.Keywords.Where(k => k == _bundleKeword2));
            Assert.True(bundle.KeywordsSearchUsingORLogic);


            return true;
        }

        private Ikiru.Parsnips.Infrastructure.Datapool.DataPoolPerson CreateDataPoolPerson()
        {
            var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
            var mapper = config.CreateMapper();

            return new Ikiru.Parsnips.Infrastructure.Datapool.DataPoolPerson(_dataPoolAPIMock.Object, mapper);
        }
    }
}
