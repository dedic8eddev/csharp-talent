using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.Functions.Functions.PersonLocationChanged;
using Ikiru.Parsnips.Functions.Maps;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues.Items;
using Ikiru.Parsnips.UnitTests.Helpers;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Storage.Queue;
using Moq;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Ikiru.Parsnips.UnitTests.Helpers.TestDataSources;
using Ikiru.Parsnips.UnitTests.Helpers.Validation;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Functions.Functions.PersonLocationChanged
{
    public class PersonLocationChangedFunctionTests
    {
        private readonly Guid m_SearchFirmId = Guid.NewGuid();
        private readonly Person m_Person;

        private readonly CloudQueueMessage m_QueueMessage;
        
        private readonly FakeCosmos m_FakeCosmos;
        private readonly Mock<IAzureMaps> m_AzureMapsApi = new Mock<IAzureMaps>();

        private readonly List<SearchAddressResponse.Result> m_MapResults;

        public PersonLocationChangedFunctionTests()
        {
            m_Person = new Person(m_SearchFirmId, Guid.NewGuid(), "https://uk.linkedin.com/in/gruffrhys")
                       {
                           Name = "Gruff Rhys",
                           JobTitle = "Lead Singer",
                           Location = "Haverfordwest, Pembrokeshire, Wales",
                           Organisation = "Super Furry Animals",

                           GdprLawfulBasisState = new PersonGdprLawfulBasisState
                                                  {
                                                      GdprDataOrigin = "Some Bloke",
                                                      GdprLawfulBasisOptionsStatus = GdprLawfulBasisOptionsStatusEnum.NotificationSent,
                                                      GdprLawfulBasisOption = GdprLawfulBasisOptionEnum.LegitimateInterest
                                                  },
                           PhoneNumbers = new List<string> { "01234 5678900", "11111, 233324" },
                           TaggedEmails = new List<TaggedEmail> { new TaggedEmail { Email = "gruff@gruffrhys.com" }, new TaggedEmail { Email = "band@superfurry.com" } },

                           Keywords = new List<string>
                                      {
                                          "Key",
                                          "Word"
                                      },
                           Documents = new List<PersonDocument>
                                       {
                                           new PersonDocument(m_SearchFirmId, "test.pdf"),
                                           new PersonDocument(m_SearchFirmId, "file.docx")
                                       },

                           SectorsIds = new List<string> { "I16721", "I150" }
                       };

            m_FakeCosmos = new FakeCosmos()
                          .EnableContainerFetch(FakeCosmos.PersonsContainerName, m_Person.Id.ToString(), m_SearchFirmId.ToString(), () => m_Person)
                          .EnableContainerReplace<Person>(FakeCosmos.PersonsContainerName, m_Person.Id.ToString(), m_SearchFirmId.ToString());

            m_QueueMessage = new CloudQueueMessage(JsonSerializer.Serialize(new PersonLocationChangedQueueItem { PersonId = m_Person.Id, SearchFirmId = m_SearchFirmId }));


            m_MapResults = new List<SearchAddressResponse.Result>
                           {
                               new SearchAddressResponse.Result
                               {
                                   address = new SearchAddressResponse.Address(),
                                   position = new SearchAddressResponse.Position()
                               },
                               new SearchAddressResponse.Result()
                           };

            m_AzureMapsApi.Setup(m => m.SearchAddress(It.IsAny<string>(), It.IsAny<string>()))
                          .Returns<string, string>((k, q) => Task.FromResult(new SearchAddressResponse { results = m_MapResults.ToArray() }));
        }

        [Fact]
        public async Task FunctionGeolocatesUsingPersonLocation()
        {
            // Given
            var function = CreateFunction();

            // When
            await function.Run(m_QueueMessage, Mock.Of<ILogger>());

            // Then
            m_AzureMapsApi.Verify(m => m.SearchAddress(It.Is<string>(k => k == "unit-test-maps-key"), It.Is<string>(q => q == m_Person.Location)));
        }

        [Theory]
        [InlineData("Chineham", "Basingstoke", "Hampshire", "England", "United Kingdom", "Chineham, Basingstoke, Hampshire, England, United Kingdom")]
        [InlineData(null, "Basingstoke", "Hampshire", "England", "United Kingdom", "Basingstoke, Hampshire, England, United Kingdom")]
        [InlineData(null, null, "Hampshire", "England", "United Kingdom", "Hampshire, England, United Kingdom")]
        [InlineData(null, "Basingstoke", null, null, "United Kingdom", "Basingstoke, United Kingdom")]
        public async Task FunctionUpdatesPersonWithGeolocation(string municipalitySubdivision, string municipality, string countrySecondarySubdivision, string countrySubdivisionName, string country, string expectedGeoDescription)
        {
            m_MapResults[0].position = new SearchAddressResponse.Position
                                       {
                                           lon = -1.0701943,
                                           lat = 51.2906229
                                       };
        
            m_MapResults[0].address = new SearchAddressResponse.Address
                                       {
                                           municipalitySubdivision = municipalitySubdivision,
                                           municipality = municipality,
                                           countrySecondarySubdivision = countrySecondarySubdivision,
                                           countrySubdivisionName = countrySubdivisionName,
                                           country = country
                                       };

            // Given
            var function = CreateFunction();

            // When
            await function.Run(m_QueueMessage, Mock.Of<ILogger>());

            // Then
            var container = m_FakeCosmos.PersonsContainer;
            
            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(p => p.Id == m_Person.Id &&
                                                                        p.SearchFirmId == m_SearchFirmId &&
                                                                        p.CreatedDate == m_Person.CreatedDate &&
                                                                        p.ImportId == m_Person.ImportId &&
                                                                        p.LinkedInProfileUrl == m_Person.LinkedInProfileUrl &&
                                                                        p.LinkedInProfileId == m_Person.LinkedInProfileId &&
                                                                        p.ImportedLinkedInProfileUrl == m_Person.ImportedLinkedInProfileUrl &&
                                                                        p.Name == m_Person.Name &&
                                                                        p.Location == m_Person.Location &&
                                                                        p.Organisation == m_Person.Organisation &&
                                                                        p.JobTitle == m_Person.JobTitle &&
                                                                        p.PhoneNumbers.IsSameList(m_Person.PhoneNumbers) &&
                                                                        p.TaggedEmails.AssertSameList(m_Person.TaggedEmails) &&
                                                                        p.Keywords.IsSameList(m_Person.Keywords) &&
                                                                        p.GdprLawfulBasisState.GdprDataOrigin == m_Person.GdprLawfulBasisState.GdprDataOrigin &&
                                                                        p.GdprLawfulBasisState.GdprLawfulBasisOption == m_Person.GdprLawfulBasisState.GdprLawfulBasisOption &&
                                                                        p.GdprLawfulBasisState.GdprLawfulBasisOptionsStatus == m_Person.GdprLawfulBasisState.GdprLawfulBasisOptionsStatus &&
                                                                        p.Documents.IsSameList(m_Person.Documents) &&
                                                                        p.SectorsIds.IsSameList(m_Person.SectorsIds) &&
                                                                        // ReSharper disable CompareOfFloatsByEqualityOperator - no float arithmetic is carried out so expect same, exact values
                                                                        p.Geolocation.Longitude == -1.0701943 &&
                                                                        p.Geolocation.Latitude == 51.2906229 &&
                                                                        // ReSharper restore CompareOfFloatsByEqualityOperator
                                                                        p.GeolocationDescription == expectedGeoDescription
                                                                        ), 
                                                     It.Is<string>(id => id == m_Person.Id.ToString()), 
                                                     It.Is<PartitionKey>(p => p == new PartitionKey(m_SearchFirmId.ToString())),
                                                     It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task FunctionUpdatesPersonWithGeolocationIfNoMatchFound()
        {
            // Given
            m_Person.SetGeolocation(-1.0, 50.0, "Some place");
            m_MapResults.Clear();
            var function = CreateFunction();

            // When
            await function.Run(m_QueueMessage, Mock.Of<ILogger>());

            // Then
            var container = m_FakeCosmos.PersonsContainer;
            
            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(p => p.Id == m_Person.Id &&
                                                                        p.Geolocation == null &&
                                                                        p.GeolocationDescription == null
                                                                        ), 
                                                     It.Is<string>(id => id == m_Person.Id.ToString()), 
                                                     It.Is<PartitionKey>(p => p == new PartitionKey(m_SearchFirmId.ToString())),
                                                     It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
        }
        
        [Theory]
        [ClassData(typeof(UnpopulatedStrings))]
        public async Task FunctionDoesNotGeolocateIfPersonLocationEmpty(string emptyLocation)
        {
            // Given
            m_Person.SetGeolocation(-1.0, 50.0, "Some place");
            m_Person.Location = emptyLocation;
            var function = CreateFunction();

            // When
            await function.Run(m_QueueMessage, Mock.Of<ILogger>());

            // Then
            var container = m_FakeCosmos.PersonsContainer;
            
            container.Verify(c => c.ReplaceItemAsync(It.Is<Person>(p => p.Id == m_Person.Id &&
                                                                        p.Geolocation == null &&
                                                                        p.GeolocationDescription == null
                                                                  ), 
                                                     It.Is<string>(id => id == m_Person.Id.ToString()), 
                                                     It.Is<PartitionKey>(p => p == new PartitionKey(m_SearchFirmId.ToString())),
                                                     It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));

            m_AzureMapsApi.Verify(m => m.SearchAddress(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        #region Private Helpers

        private PersonLocationChangedFunction CreateFunction()
        {
            return new FunctionBuilder<PersonLocationChangedFunction>()
                  .SetFakeCosmos(m_FakeCosmos)
                  .AddTransient(m_AzureMapsApi.Object)
                  .Build();
        }

        #endregion
    }
}