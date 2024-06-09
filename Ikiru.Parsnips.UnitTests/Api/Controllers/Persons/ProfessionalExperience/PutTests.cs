using Ikiru.Parsnips.Api.Controllers.Persons.ProfessionalExperience;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.UnitTests.Helpers;
using Ikiru.Parsnips.UnitTests.Helpers.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Put = Ikiru.Parsnips.Api.Controllers.Persons.ProfessionalExperience.Put;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Persons.ProfessionalExperience
{
    public class PutTests
    {
        private readonly Guid m_SearchFirmId = Guid.NewGuid();
        private const string _NEW_KEYWORD1 = "keyword1";
        private const string _NEW_KEYWORD2 = "keyword2";

        private readonly Put.Command m_Command = new Put.Command
        {
            Sectors = new List<Put.Command.RequestSector>
                        {
                            new Put.Command.RequestSector { SectorId = "I1211" },
                            new Put.Command.RequestSector { SectorId = "I121" }
                        },
            Keywords = new List<string>
                        {
                            _NEW_KEYWORD1,
                            _NEW_KEYWORD2
                        }
        };

        private readonly Person m_Person;
        private readonly Guid m_MissingPersonId = Guid.NewGuid();

        private readonly FakeCosmos m_FakeCosmos;

        public PutTests()
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
                          .EnableContainerReplace<Person>(FakeCosmos.PersonsContainerName, m_Person.Id.ToString(), m_SearchFirmId.ToString())
                          .EnableContainerFetchThrowCosmosException<Person>(FakeCosmos.PersonsContainerName, m_MissingPersonId.ToString(), m_SearchFirmId.ToString(), HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task PutUpdatesItemInContainer()
        {
            // Given
            var controller = CreateController();

            // When
            await controller.Put(m_Person.Id, m_Command);

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
                                                                        p.Keywords.IsSameList(m_Command.Keywords) &&
                                                                        p.GdprLawfulBasisState.GdprDataOrigin == m_Person.GdprLawfulBasisState.GdprDataOrigin &&
                                                                        p.GdprLawfulBasisState.GdprLawfulBasisOption == m_Person.GdprLawfulBasisState.GdprLawfulBasisOption &&
                                                                        p.GdprLawfulBasisState.GdprLawfulBasisOptionsStatus == m_Person.GdprLawfulBasisState.GdprLawfulBasisOptionsStatus &&
                                                                        p.Documents.IsSameList(m_Person.Documents) &&
                                                                        p.SectorsIds.IsSameList(m_Command.Sectors.Select(s => s.SectorId).ToList())                                                                        
                                                                        ), 
                                                     It.Is<string>(id => id == m_Person.Id.ToString()), 
                                                     It.Is<PartitionKey>(p => p == new PartitionKey(m_SearchFirmId.ToString())),
                                                     It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task PutReturnsUpdatedResource()
        {
            // Given
            var controller = CreateController();

            // When
            var actionResult = await controller.Put(m_Person.Id, m_Command);

            // Then
            var result = (Put.Result)((OkObjectResult)actionResult).Value;

            Assert.NotNull(result.Sectors);
            Assert.Equal(m_Command.Sectors.Count, result.Sectors.Count);
            var firstSector = result.Sectors[0];
            Assert.Equal(m_Command.Sectors[0].SectorId, firstSector.SectorId);
            Assert.NotNull(firstSector.LinkSector);
            Assert.Equal("Aerospace", firstSector.LinkSector.Name);
            
            var secondSector = result.Sectors[1];
            Assert.Equal(m_Command.Sectors[1].SectorId, secondSector.SectorId);
            Assert.NotNull(secondSector.LinkSector);
            Assert.Equal("Aerospace and Defence", secondSector.LinkSector.Name);
        }

        [Fact]
        public async Task PutThrowsIfPersonNotExists()
        {
            // Given
            var controller = CreateController();

            // When
            var ex = await Record.ExceptionAsync(() => controller.Put(m_MissingPersonId, m_Command));

            // Then
            ex.AssertNotFoundFailure($"Unable to find 'Person' with Id '{m_MissingPersonId}'");
        }
        
        [Fact]
        public async Task PutThrowsIfSectorNotExists()
        {
            // Given
            m_Command.Sectors.Add(new Put.Command.RequestSector { SectorId = "Some Rubbish" });
            var controller = CreateController();

            // When
            var ex = await Record.ExceptionAsync(() => controller.Put(m_Person.Id, m_Command));

            // Then
            ex.AssertParamValidationFailure("Sectors", "Invalid Sectors: 'Some Rubbish'");
        }
        
        private ProfessionalExperienceController CreateController()
            => new ControllerBuilder<ProfessionalExperienceController>()
              .SetSearchFirmUser(m_SearchFirmId)
              .SetFakeCosmos(m_FakeCosmos)
              .SetFakeRepository(new FakeRepository())
              .Build();
    }
}
