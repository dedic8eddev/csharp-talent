using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Ikiru.Parsnips.IntegrationTests.Helpers;
using Ikiru.Parsnips.IntegrationTests.Helpers.Data;
using Ikiru.Parsnips.IntegrationTests.Helpers.HttpMessages;
using Ikiru.Parsnips.Domain;
using Xunit;
using Xunit.Abstractions;

namespace Ikiru.Parsnips.IntegrationTests.Features.Api.Persons.ProfessionalExperience
{
    [Collection(nameof(IntegrationTestCollection))]
    public class ProfessionalExperienceTests : IntegrationTestBase, IClassFixture<ProfessionalExperienceTests.KeywordsTestsClassFixture>
    {
        private readonly KeywordsTestsClassFixture m_ClassFixture;

        public ProfessionalExperienceTests(IntegrationTestFixture fixture, KeywordsTestsClassFixture classFixture, ITestOutputHelper output) : base(fixture, output)
        {
            m_ClassFixture = classFixture;
        }

        public class KeywordsTestsClassFixture : IDisposable
        {
            public IntTestServer Server { get; }

            // ReSharper disable once UnusedParameter.Local
            public KeywordsTestsClassFixture(IntegrationTestFixture fixture)
            {
                Server = new TestServerBuilder()
                   .Build();
            }

            public void Dispose()
            {
                Server.Dispose();
            }
        }

        private Person m_Person;

        private async Task AddPersonToCosmos()
        {
            m_Person = new Person(m_ClassFixture.Server.Authentication.DefaultSearchFirmId)
            {
                Name = "Prof Exp Person",
                SectorsIds = new List<string>{ "I1269", "I103" },
                Keywords = new List<string> { "K1", "K2" }
            };

            m_Person = await m_ClassFixture.Server.InsertItemIntoCosmos(TestDataManipulator.PersonsContainerName, m_Person.SearchFirmId, m_Person);
        }

        [Fact]
        public async Task GetShouldRespondWithOkResult()
        {
            // GIVEN
            await AddPersonToCosmos();

            // WHEN
            var response = await m_ClassFixture.Server.Client.GetAsync($"/api/persons/{m_Person.Id}/professionalExperience?expand=sector");

            // THEN
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);

            var r = new
                    {
                        Sectors = new[]
                                  {
                                      new
                                      {
                                          SectorId = "",
                                          LinkSector = new
                                                       {
                                                           SectorId = "",
                                                           Name = ""
                                                       }
                                      }
                                  },
                        Keywords = new List<string> {}
                    };
            
            var responseJson = await response.Content.DeserializeToAnonymousType(r);

            Assert.NotNull(responseJson.Sectors);
            Assert.Equal(2, responseJson.Sectors.Length);
            Assert.Equal("I1269", responseJson.Sectors[0].SectorId);
            Assert.Equal("I1269", responseJson.Sectors[0].LinkSector.SectorId);
            Assert.Equal("Automation Technology", responseJson.Sectors[0].LinkSector.Name);
            Assert.Equal("I103", responseJson.Sectors[1].SectorId);
            Assert.Equal("I103", responseJson.Sectors[1].LinkSector.SectorId);
            Assert.Equal("Clothing & Accessories", responseJson.Sectors[1].LinkSector.Name);
            Assert.Equal("K1", responseJson.Keywords[0]);
            Assert.Equal("K2", responseJson.Keywords[1]);
        }

        [Fact]
        public async Task PutShouldRespondWithOkResult()
        {
            // GIVEN
            await AddPersonToCosmos();
            var command = new
                          {
                              Sectors = new[]
                                        {
                                            new
                                            {
                                                SectorId = "I1683"
                                            },
                                            new
                                            {
                                                SectorId = "I1021"
                                            }
                                        },
                              Keywords = new[]
                                         {
                                            "keyword1",
                                            "keyword2"
                                         }
                          };

            // WHEN
            var response = await m_ClassFixture.Server.Client.PutAsync($"/api/persons/{m_Person.Id}/professionalExperience", new JsonContent(command));

            // THEN
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);

            var r = new
                    {
                        Sectors = new[]
                                  {
                                      new
                                      {
                                          SectorId = "",
                                          LinkSector = new
                                                       {
                                                           SectorId = "",
                                                           Name = ""
                                                       }
                                      }
                                  },
                        Keywords = new List<string> { }
            };
            
            var responseJson = await response.Content.DeserializeToAnonymousType(r);

            Assert.NotNull(responseJson.Sectors);
            Assert.Equal(2, responseJson.Sectors.Length);
            Assert.Equal("I1683", responseJson.Sectors[0].SectorId);
            Assert.Equal("I1683", responseJson.Sectors[0].LinkSector.SectorId);
            Assert.Equal("Blockchain/Bitcoin", responseJson.Sectors[0].LinkSector.Name);
            Assert.Equal("I1021", responseJson.Sectors[1].SectorId);
            Assert.Equal("I1021", responseJson.Sectors[1].LinkSector.SectorId);
            Assert.Equal("Brewers", responseJson.Sectors[1].LinkSector.Name);
            Assert.Equal("keyword1", responseJson.Keywords[0]);
            Assert.Equal("keyword2", responseJson.Keywords[1]);

        }
    }
}