using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ikiru.Parsnips.Api.Controllers.Persons.Import;
using Ikiru.Parsnips.UnitTests.Helpers;
using Ikiru.Parsnips.UnitTests.Helpers.TestDataSources;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Persons.Import
{
    public class GetTests
    {
        private readonly Guid m_SearchFirmId = Guid.NewGuid();

        private string m_StoredLinkedInProfileId = "default-unit-test-profile-id";
        private string StoredLinkedInProfileUrl => $"https://www.linkedin.com/in/{m_StoredLinkedInProfileId}";

        private readonly Get.Query m_Command = new Get.Query();

        private readonly FakeCosmos m_FakeCosmos = new FakeCosmos();

        public GetTests()
        {
            // Should Import begin returning data from the GET endpoint, see GET person for Seed Query pattern.
            m_FakeCosmos.EnableContainerLinqQuery(FakeCosmos.ImportsContainerName, m_SearchFirmId.ToString(), () => new List<Domain.Import> 
                                                                                                                    { 
                                                                                                                        new Domain.Import(Guid.Empty, StoredLinkedInProfileUrl) // Deferred
                                                                                                                    }
                                                  );
        }
        
        [Theory]
        [ClassData(typeof(ValidLinkedInProfileUrlNormalisations))]
        public async Task GetReturnsNoContentIfMatch(string validLinkedInProfileUrl, string profileId)
        {
            // Given
            m_StoredLinkedInProfileId = profileId;
            m_Command.LinkedInProfileUrl = validLinkedInProfileUrl;
            var controller = CreateController();

            // When
            var actionResult = await controller.Get(m_Command);

            // Then
            Assert.IsType<NoContentResult>(actionResult);
        }

        [Fact]
        public async Task GetReturnsThrowsNotFoundIfNoMatch()
        {
            // Given
            m_Command.LinkedInProfileUrl = "https://uk.linkedin.com/in/doesnt-exist-in-data-store";
            var controller = CreateController();

            // When
            var ex = await Record.ExceptionAsync(() => controller.Get(m_Command));

            // Then
            ex.AssertNotFoundFailure($"Unable to find 'Import' with LinkedInProfileUrl '{m_Command.LinkedInProfileUrl}'");
        }

        private ImportController CreateController()
        {
            return new ControllerBuilder<ImportController>()
                  .SetFakeCosmos(m_FakeCosmos)
                  .SetSearchFirmUser(m_SearchFirmId)
                  .Build();
        }
    }
}
