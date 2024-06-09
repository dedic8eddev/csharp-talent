using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Functions.Functions.Import;
using Ikiru.Parsnips.Functions.Parsing.Api;
using Ikiru.Parsnips.Functions.Parsing.Api.Models;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Blobs;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues.Items;
using Ikiru.Parsnips.UnitTests.Helpers;
using Ikiru.Parsnips.UnitTests.Helpers.TestDataSources;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Storage.Queue;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ikiru.Parsnips.UnitTests.Helpers.Validation;
using Xunit;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Ikiru.Parsnips.UnitTests.Functions.Functions.Import
{
    public class ImportFunctionTests
    {
        private CloudQueueMessage m_CloudQueueMessage;
        private PersonFileUploadQueueItem m_PersonFileUploadQueueItem;

        private string m_BlobLinkedInProfileUrl = "https://www.linkedin.com/in/default-unit-test-profile-id/";
        private string m_BlobContentType = "application/pdf";
        private readonly Guid m_BlobSearchFirmId = Guid.NewGuid();

        private const string _EXCEPTION_MESSAGE = "I cannot save this today!";

        private readonly Guid m_ImportIdThatWillThrow = Guid.NewGuid();

        private Guid m_BlobImportId = Guid.NewGuid();

        private readonly List<Person> m_QueryResult = new List<Person>();
        private readonly SovrenResponse m_ParseResponse;
        private readonly SovrenParsedDocument m_SovrenParsedDocument;
        private readonly Mock<ISovrenApi> m_ParsingApi;

        private readonly FakeCosmos m_FakeCosmos = new FakeCosmos()
           .EnableContainerInsert<Person>(FakeCosmos.PersonsContainerName);

        private readonly FakeCloud m_FakeCloud = new FakeCloud();
        private readonly FakeTelemetry m_Telemetry = new FakeTelemetry();
        private readonly FakeStorageQueue m_FakeStorageQueue = new FakeStorageQueue();

        private readonly Mock<ILogger> m_MockLogger = new Mock<ILogger>();


        public ImportFunctionTests()
        {
            m_FakeCosmos.EnableContainerLinqQuery(FakeCosmos.PersonsContainerName, m_BlobSearchFirmId.ToString(), () => m_QueryResult);

            m_ParseResponse = new SovrenResponse
            {
                Info = new SovrenInfo
                {
                    Code = InfoCode.Success
                },
                Value = new SovrenValue
                {
                    ParsedDocument = ""
                }
            };
            m_SovrenParsedDocument = CreateFullParsedDocumentResult();

            m_ParsingApi = new Mock<ISovrenApi>();
            m_ParsingApi.Setup(a => a.ParseCv(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SovrenRequest>()))
                        .ReturnsAsync(m_ParseResponse);

            var mockBlobClient = new Mock<BlobClient>();
            mockBlobClient.Setup(c => c.UploadAsync(It.IsAny<Stream>()))
                          .ThrowsAsync(new Exception(_EXCEPTION_MESSAGE));

            m_PersonFileUploadQueueItem = new PersonFileUploadQueueItem
                                          {
                                              BlobName = $"{m_BlobSearchFirmId}/{m_BlobImportId}",
                                              ContainerName = BlobStorage.ContainerNames.RawResumes
                                          };

            m_FakeCloud.BlobClients[$"{BlobStorage.ContainerNames.RawResumes}/{m_BlobSearchFirmId}/{m_ImportIdThatWillThrow}"] = mockBlobClient;
            m_FakeCloud.BlobClients[$"{m_PersonFileUploadQueueItem.ContainerName}/{m_PersonFileUploadQueueItem.BlobName}"] = CreateDefaultStorageBlob();

            m_CloudQueueMessage = new CloudQueueMessage(JsonConvert.SerializeObject(m_PersonFileUploadQueueItem));

        }

        [Theory]
        [ClassData(typeof(ValidLinkedInProfileUrlNormalisations))]
        public async Task FunctionDoesNotAddPersonIfPersonAlreadyPresent(string validLinkedInProfileUrl, string profileId)
        {
            // Given
            m_BlobLinkedInProfileUrl = validLinkedInProfileUrl;
            m_QueryResult.Add(new Person(m_BlobSearchFirmId, Guid.NewGuid(), $"https://www.linkedin.com/in/{profileId}/"));

            var fileContents = await ReadFileAsBase64(".\\Functions\\Import\\test.profile.json");

            m_FakeCloud.BlobClients[$"{m_PersonFileUploadQueueItem.ContainerName}/{m_PersonFileUploadQueueItem.BlobName}"] =
                CreateStorageBlobFromBase64StringContent(fileContents);

            // When
            await CreateFunction().Run(m_CloudQueueMessage, Mock.Of<ILogger>());


            // Then
            var container = m_FakeCosmos.PersonsContainer;
            container.Verify(c => c.CreateItemAsync(It.IsAny<Person>(), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task FunctionSendsRequestToSovren()
        {
            // Given
            // Sovren settings are set in the appsettings.json
            var base64Content = CreateBase64ContentFromString("some blob content");

            m_FakeCloud.BlobClients[$"{m_PersonFileUploadQueueItem.ContainerName}/{m_PersonFileUploadQueueItem.BlobName}"] =
                CreateStorageBlobFromBase64StringContent(base64Content);

            // When
            await CreateFunction().Run(m_CloudQueueMessage, Mock.Of<ILogger>());

            // Then
            m_ParsingApi.Verify(a => a.ParseCv(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SovrenRequest>()), Times.Once);
            m_ParsingApi.Verify(a => a.ParseCv(It.Is<string>(s => s == "sovrenAccountId1234"), It.IsAny<string>(), It.IsAny<SovrenRequest>()));
            m_ParsingApi.Verify(a => a.ParseCv(It.IsAny<string>(), It.Is<string>(s => s == "sovrenAccountKeyAbc"), It.IsAny<SovrenRequest>()));
            m_ParsingApi.Verify(a => a.ParseCv(It.IsAny<string>(), It.IsAny<string>(), It.Is<SovrenRequest>(r => r.RevisionDate == DateTimeOffset.Now.ToString("yyyy-MM-dd"))));
            m_ParsingApi.Verify(a => a.ParseCv(It.IsAny<string>(), It.IsAny<string>(), It.Is<SovrenRequest>(r => r.DocumentAsBase64String == base64Content)));
        }

        [Fact]
        public async Task FunctionDoesNotSendRequestToSovrenIfJsonImport()
        {
            // Given
            m_BlobContentType = "application/json";
            var base64Content = await ReadFileAsBase64(".\\Functions\\Import\\test.profile.json");
            m_FakeCloud.BlobClients[$"{m_PersonFileUploadQueueItem.ContainerName}/{m_PersonFileUploadQueueItem.BlobName}"] =
                CreateStorageBlobFromBase64StringContent(base64Content);

            // When
            await CreateFunction().Run(m_CloudQueueMessage, Mock.Of<ILogger>());

            // Then
            m_ParsingApi.Verify(a => a.ParseCv(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SovrenRequest>()), Times.Never);
        }

        // Need to run the FunctionCreatesItemInContainer with both Json and non-Json
        public class ValidLinkedInProfileUrlNormalisationsWithContentType : ValidLinkedInProfileUrlNormalisations
        {
            protected override IEnumerator<object[]> GetValues()
            {
                var values = base.GetValues();
                while (values.MoveNext())
                {
                    yield return new List<object>(values.Current) { "application/pdf" }.ToArray();
                    yield return new List<object>(values.Current) { "application/json" }.ToArray();
                }
            }
        }

        [Theory]
        [ClassData(typeof(ValidLinkedInProfileUrlNormalisationsWithContentType))]
        public async Task FunctionCreatesItemInContainer(string validLinkedInProfileUrl, string profileId, string blobContentType)
        {
            // Given
            m_BlobLinkedInProfileUrl = validLinkedInProfileUrl;
            m_BlobContentType = blobContentType;
            if (blobContentType == "application/json")
            {
                var base64Content = await ReadFileAsBase64(".\\Functions\\Import\\test.profile.json");
                m_FakeCloud.BlobClients[$"{m_PersonFileUploadQueueItem.ContainerName}/{m_PersonFileUploadQueueItem.BlobName}"] =
                    CreateStorageBlobFromBase64StringContent(base64Content);
            }
            else
            {
                m_FakeCloud.BlobClients[$"{m_PersonFileUploadQueueItem.ContainerName}/{m_PersonFileUploadQueueItem.BlobName}"] =
                    CreateDefaultStorageBlob();
            }

            // When
            await CreateFunction().Run(m_CloudQueueMessage, Mock.Of<ILogger>());

            // Then
            var container = m_FakeCosmos.PersonsContainer;
            container.Verify(c => c.CreateItemAsync(It.IsAny<Person>(), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()), Times.Once);
            container.Verify(c => c.CreateItemAsync(It.IsAny<Person>(), It.Is<PartitionKey?>(p => p == new PartitionKey(m_BlobSearchFirmId.ToString())), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.CreateItemAsync(It.Is<Person>(p => p.Id != Guid.Empty), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.CreateItemAsync(It.Is<Person>(p => p.CreatedDate.Date == DateTime.UtcNow.Date), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.CreateItemAsync(It.Is<Person>(p => p.SearchFirmId == m_BlobSearchFirmId), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.CreateItemAsync(It.Is<Person>(p => p.ImportId == m_BlobImportId), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.CreateItemAsync(It.Is<Person>(p => p.LinkedInProfileId == profileId), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
            container.Verify(c => c.CreateItemAsync(It.Is<Person>(p => p.LinkedInProfileUrl == validLinkedInProfileUrl), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
        }

        #region Sovren Response Data to Person Properties

        public static IEnumerable<object[]> ParsedDocumentNameCases()
        {
            yield return new object[] { new Action<Structuredxmlresume>(r => r.ContactInfo.PersonName.FormattedName = ""), "" };
            yield return new object[] { new Action<Structuredxmlresume>(r => r.ContactInfo.PersonName.FormattedName = "Raymond Parlour"), "Raymond Parlour" };
            yield return new object[] { new Action<Structuredxmlresume>(r => r.ContactInfo.PersonName.FormattedName = null), "" };
            yield return new object[] { new Action<Structuredxmlresume>(r => r.ContactInfo.PersonName = null), "" };
            yield return new object[] { new Action<Structuredxmlresume>(r => r.ContactInfo = null), "" };
        }

        [Theory]
        [MemberData(nameof(ParsedDocumentNameCases))]
        public async Task FunctionCreatesItemWithCorrectName(Action<Structuredxmlresume> setupParsedDocument, string expectedName)
        {
            // Given
            setupParsedDocument(m_SovrenParsedDocument.Resume.StructuredXMLResume);

            // When
            await CreateFunction().Run(m_CloudQueueMessage, Mock.Of<ILogger>());

            // Then
            var container = m_FakeCosmos.PersonsContainer;
            container.Verify(c => c.CreateItemAsync(It.Is<Person>(p => p.Name == expectedName), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
        }



        public static IEnumerable<object[]> ParsedDocumentLinkedInProfileUrlCases()
        {
            yield return new object[] { new Action<Structuredxmlresume>(r => r.ContactInfo.ContactMethod.Single(c => c.Use == "linkedIn").InternetWebAddress = ""), "" };
            yield return new object[] { new Action<Structuredxmlresume>(r => r.ContactInfo.ContactMethod.Single(c => c.Use == "linkedIn").InternetWebAddress = "https://www.linkedin.com/in/voyager-training-219196134/"), "https://www.linkedin.com/in/voyager-training-219196134/" };
            yield return new object[] { new Action<Structuredxmlresume>(r => r.ContactInfo.ContactMethod.Single(c => c.Use == "linkedIn").Use = "businessDirect"), null };
            yield return new object[] { new Action<Structuredxmlresume>(r => r.ContactInfo.ContactMethod = null), null };
            yield return new object[] { new Action<Structuredxmlresume>(r => r.ContactInfo = null), null };
        }

        [Theory]
        [MemberData(nameof(ParsedDocumentLinkedInProfileUrlCases))]
        public async Task FunctionCreatesItemWithCorrectLinkedInProfileUrl(Action<Structuredxmlresume> setupParsedDocument, string expectedLinkedInProfileUrl)
        {
            // Given
            setupParsedDocument(m_SovrenParsedDocument.Resume.StructuredXMLResume);

            // When
            await CreateFunction().Run(m_CloudQueueMessage, Mock.Of<ILogger>());

            // Then
            var container = m_FakeCosmos.PersonsContainer;
            container.Verify(c => c.CreateItemAsync(It.Is<Person>(p => p.ImportedLinkedInProfileUrl == expectedLinkedInProfileUrl), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
        }



        public static IEnumerable<object[]> ParsedDocumentEmailCases()
        {
            yield return new object[] { new Action<Structuredxmlresume>(r => r.ContactInfo.ContactMethod.Single(c => c.InternetEmailAddress != null).InternetEmailAddress = ""), new List<TaggedEmail>() };
            yield return new object[] { new Action<Structuredxmlresume>(r => r.ContactInfo.ContactMethod.Single(c => c.InternetEmailAddress != null).InternetEmailAddress = " "), new List<TaggedEmail>() };
            yield return new object[] { new Action<Structuredxmlresume>(r => r.ContactInfo.ContactMethod.Single(c => c.InternetEmailAddress != null).InternetEmailAddress = "ray@parlour.pub"), new List<TaggedEmail> { new TaggedEmail { Email = "ray@parlour.pub", SmtpValid = "false"} } };
            yield return new object[] { new Action<Structuredxmlresume>(r => r.ContactInfo.ContactMethod.Single(c => c.InternetEmailAddress != null).InternetEmailAddress = null), new List<TaggedEmail>() };
            yield return new object[] { new Action<Structuredxmlresume>(r => r.ContactInfo.ContactMethod = null), new List<TaggedEmail>() };
            yield return new object[] { new Action<Structuredxmlresume>(r => r.ContactInfo = null), new List<TaggedEmail>() };
        }

        [Theory]
        [MemberData(nameof(ParsedDocumentEmailCases))]
        public async Task FunctionCreatesItemWithCorrectEmail(Action<Structuredxmlresume> setupParsedDocument, List<TaggedEmail> expectedEmailAddresses)
        {
            // Given
            setupParsedDocument(m_SovrenParsedDocument.Resume.StructuredXMLResume);

            // When
            await CreateFunction().Run(m_CloudQueueMessage, Mock.Of<ILogger>());

            // Then
            var container = m_FakeCosmos.PersonsContainer;
            container.Verify(c => c.CreateItemAsync(It.Is<Person>(p => p.TaggedEmails.AssertSameList(expectedEmailAddresses)), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
        }



        public static IEnumerable<object[]> ParsedDocumentPhoneNumberCases()
        {
            yield return new object[] { new Action<Structuredxmlresume>(r => r.ContactInfo.ContactMethod.Single(c => c.Mobile != null).Mobile.FormattedNumber = "01234987654"), new List<string> { "01234987654" } };
            yield return new object[] { new Action<Structuredxmlresume>(r =>
                                                                        {
                                                                            r.ContactInfo.ContactMethod.Single(c => c.Mobile != null).Mobile.FormattedNumber = "01234111222";
                                                                            r.ContactInfo.ContactMethod.Single(c => c.Telephone != null).Telephone.FormattedNumber = " ";
                                                                        }), new List<string> { "01234111222" } };
            yield return new object[] { new Action<Structuredxmlresume>(r => r.ContactInfo.ContactMethod.Single(c => c.Telephone != null).Telephone.FormattedNumber = "09876123456"), new List<string> { "09876123456" } };
            yield return new object[] { new Action<Structuredxmlresume>(r =>
                                                                        {
                                                                            r.ContactInfo.ContactMethod.Single(c => c.Mobile != null).Mobile.FormattedNumber = "01234333444";
                                                                            r.ContactInfo.ContactMethod.Single(c => c.Telephone != null).Telephone.FormattedNumber = "09876555666";
                                                                        }), new List<string> { "09876555666" } };
            yield return new object[] { new Action<Structuredxmlresume>(r =>
                                                                        {
                                                                            r.ContactInfo.ContactMethod.Single(c => c.Mobile != null).Mobile.FormattedNumber = null;
                                                                            r.ContactInfo.ContactMethod.Single(c => c.Telephone != null).Telephone.FormattedNumber = null;
                                                                        }), new List<string>() };
            yield return new object[] { new Action<Structuredxmlresume>(r =>
                                                                        {
                                                                            r.ContactInfo.ContactMethod.Single(c => c.Mobile != null).Mobile = null;
                                                                        }), new List<string>() };
            yield return new object[] { new Action<Structuredxmlresume>(r =>
                                                                        {
                                                                            r.ContactInfo.ContactMethod.Single(c => c.Telephone != null).Telephone = null;
                                                                        }), new List<string>() };
            yield return new object[] { new Action<Structuredxmlresume>(r => r.ContactInfo.ContactMethod = null), new List<string>() };
            yield return new object[] { new Action<Structuredxmlresume>(r => r.ContactInfo = null), new List<string>() };
        }

        [Theory]
        [MemberData(nameof(ParsedDocumentPhoneNumberCases))]
        public async Task FunctionCreatesItemWithCorrectPhoneNumber(Action<Structuredxmlresume> setupParsedDocument, List<string> expectedPhoneNumbers)
        {
            // Given
            setupParsedDocument(m_SovrenParsedDocument.Resume.StructuredXMLResume);

            // When
            await CreateFunction().Run(m_CloudQueueMessage, Mock.Of<ILogger>());

            // Then
            var container = m_FakeCosmos.PersonsContainer;
            container.Verify(c => c.CreateItemAsync(It.Is<Person>(p => p.PhoneNumbers.IsSameList(expectedPhoneNumbers)), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
        }



        public static IEnumerable<object[]> ParsedDocumentLocationCases()
        {
            yield return new object[] { new Action<Structuredxmlresume>(r => r.ContactInfo.ContactMethod.Single(c => c.PostalAddress != null).PostalAddress.Municipality = "Basingstoke"), "Basingstoke" };
            yield return new object[] { new Action<Structuredxmlresume>(r => r.ContactInfo.ContactMethod.Single(c => c.PostalAddress != null).PostalAddress.Region[0] = "Hampshire"), "Hampshire" };
            yield return new object[] { new Action<Structuredxmlresume>(r => r.ContactInfo.ContactMethod.Single(c => c.PostalAddress != null).PostalAddress.CountryCode = "UK"), "UK" };
            yield return new object[] { new Action<Structuredxmlresume>(r =>
                                                                        {
                                                                            var postalAddress = r.ContactInfo.ContactMethod.Single(c => c.PostalAddress != null).PostalAddress;
                                                                            postalAddress.Municipality = "Basingstoke";
                                                                            postalAddress.Region[0] = "Hampshire";
                                                                            postalAddress.CountryCode = "UK";
                                                                        }), "Basingstoke, Hampshire, UK" };

            yield return new object[] { new Action<Structuredxmlresume>(r => r.ContactInfo.ContactMethod.Single(c => c.PostalAddress != null).PostalAddress = null), null };
            yield return new object[] { new Action<Structuredxmlresume>(r => r.ContactInfo.ContactMethod = null), null };
            yield return new object[] { new Action<Structuredxmlresume>(r => r.ContactInfo = null), null };
        }

        [Theory]
        [MemberData(nameof(ParsedDocumentLocationCases))]
        public async Task FunctionCreatesItemWithCorrectLocation(Action<Structuredxmlresume> setupParsedDocument, string expectedLocation)
        {
            // Given
            setupParsedDocument(m_SovrenParsedDocument.Resume.StructuredXMLResume);

            // When
            await CreateFunction().Run(m_CloudQueueMessage, Mock.Of<ILogger>());

            // Then
            var container = m_FakeCosmos.PersonsContainer;
            container.Verify(c => c.CreateItemAsync(It.Is<Person>(p => p.Location == expectedLocation), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
        }



        public static IEnumerable<object[]> ParsedDocumentOrganisationCases()
        {
            yield return new object[] { new Action<Structuredxmlresume>(r => r.EmploymentHistory.EmployerOrg.First().EmployerOrgName = ""), "" };
            yield return new object[] { new Action<Structuredxmlresume>(r => r.EmploymentHistory.EmployerOrg.First().EmployerOrgName = "Acme Explosions Inc."), "Acme Explosions Inc." };
            yield return new object[] { new Action<Structuredxmlresume>(r => r.EmploymentHistory.EmployerOrg.First().EmployerOrgName = null), null };
            yield return new object[] { new Action<Structuredxmlresume>(r => r.EmploymentHistory.EmployerOrg = new Employerorg[0]), null };
            yield return new object[] { new Action<Structuredxmlresume>(r => r.EmploymentHistory.EmployerOrg = null), null };
            yield return new object[] { new Action<Structuredxmlresume>(r => r.EmploymentHistory = null), null };
        }

        [Theory]
        [MemberData(nameof(ParsedDocumentOrganisationCases))]
        public async Task FunctionCreatesItemWithCorrectOrganisation(Action<Structuredxmlresume> setupParsedDocument, string expectedOrganisation)
        {
            // Given
            setupParsedDocument(m_SovrenParsedDocument.Resume.StructuredXMLResume);

            // When
            await CreateFunction().Run(m_CloudQueueMessage, Mock.Of<ILogger>());

            // Then
            var container = m_FakeCosmos.PersonsContainer;
            container.Verify(c => c.CreateItemAsync(It.Is<Person>(p => p.Organisation == expectedOrganisation), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
        }



        public static IEnumerable<object[]> ParsedDocumentJobTitleCases()
        {
            yield return new object[] { new Action<Structuredxmlresume>(r => r.EmploymentHistory.EmployerOrg.First().PositionHistory.First().Title = ""), "" };
            yield return new object[] { new Action<Structuredxmlresume>(r => r.EmploymentHistory.EmployerOrg.First().PositionHistory.First().Title = "Weapons Tester"), "Weapons Tester" };
            yield return new object[] { new Action<Structuredxmlresume>(r => r.EmploymentHistory.EmployerOrg.First().PositionHistory.First().Title = null), null };
            yield return new object[] { new Action<Structuredxmlresume>(r => r.EmploymentHistory.EmployerOrg.First().PositionHistory = new Positionhistory[0]), null };
            yield return new object[] { new Action<Structuredxmlresume>(r => r.EmploymentHistory.EmployerOrg.First().PositionHistory = null), null };
            yield return new object[] { new Action<Structuredxmlresume>(r => r.EmploymentHistory.EmployerOrg = new Employerorg[0]), null };
            yield return new object[] { new Action<Structuredxmlresume>(r => r.EmploymentHistory.EmployerOrg = null), null };
            yield return new object[] { new Action<Structuredxmlresume>(r => r.EmploymentHistory = null), null };
        }

        [Theory]
        [MemberData(nameof(ParsedDocumentJobTitleCases))]
        public async Task FunctionCreatesItemWithCorrectJobTitle(Action<Structuredxmlresume> setupParsedDocument, string expectedJobTitle)
        {
            // Given
            setupParsedDocument(m_SovrenParsedDocument.Resume.StructuredXMLResume);

            // When
            await CreateFunction().Run(m_CloudQueueMessage, Mock.Of<ILogger>());

            // Then
            var container = m_FakeCosmos.PersonsContainer;
            container.Verify(c => c.CreateItemAsync(It.Is<Person>(p => p.JobTitle == expectedJobTitle), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task FunctionStoresSovrenOutput()
        {
            // Given
            m_SovrenParsedDocument.Resume.StructuredXMLResume.EmploymentHistory.EmployerOrg.First().PositionHistory.First().Title = "Weapons Tester";
            m_SovrenParsedDocument.Resume.StructuredXMLResume.EmploymentHistory.EmployerOrg.First().EmployerOrgName = "Acme Explosions Inc.";
            m_SovrenParsedDocument.Resume.StructuredXMLResume.ContactInfo.ContactMethod.Single(c => c.PostalAddress != null).PostalAddress = new Postaladdress { Municipality = "Basingstoke", Region = new[] { "Hampshire" }, CountryCode = "UK" };
            m_SovrenParsedDocument.Resume.StructuredXMLResume.ContactInfo.ContactMethod.Single(c => c.Mobile != null).Mobile.FormattedNumber = "01234111222";
            m_SovrenParsedDocument.Resume.StructuredXMLResume.ContactInfo.ContactMethod.Single(c => c.InternetEmailAddress != null).InternetEmailAddress = "ray@parlour.pub";
            m_SovrenParsedDocument.Resume.StructuredXMLResume.ContactInfo.ContactMethod.Single(c => c.Use == "linkedIn").InternetWebAddress = "https://www.linkedin.com/in/voyager-training-219196134/";
            m_SovrenParsedDocument.Resume.StructuredXMLResume.ContactInfo.PersonName.FormattedName = "Raymond Parlour";

            // When
            await CreateFunction().Run(m_CloudQueueMessage, Mock.Of<ILogger>());

            // Then
            var expectedBlobPath = $"{BlobStorage.ContainerNames.RawResumes}/{m_BlobSearchFirmId}/{m_BlobImportId}";
            Assert.True(m_FakeCloud.BlobClients.ContainsKey(expectedBlobPath));

            var serializedResponse = JsonSerializer.Serialize(m_ParseResponse);

            m_FakeCloud.BlobClients[expectedBlobPath].Verify(b => b.UploadAsync(
                                                It.Is<Stream>(s => Encoding.UTF8.GetString(((MemoryStream)s).ToArray()) == serializedResponse),
                                                null, null, default, default, default, default, default));
        }

        [Fact]
        public async Task FunctionStillCreatesItemInContainerIfStoreSovrenOutputFails()
        {
            // Given
            m_BlobImportId = m_ImportIdThatWillThrow;

            // When
            var ex = await Record.ExceptionAsync(() => CreateFunction().Run(m_CloudQueueMessage, m_MockLogger.Object));

            // Then
            Assert.Null(ex);

            var container = m_FakeCosmos.PersonsContainer;
            container.Verify(c => c.CreateItemAsync(It.IsAny<Person>(), It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
        }

        #endregion

        #region Json Data to Person Properties

        [Fact]
        public async Task FunctionCreatesItemWithCorrectDataFromJson()
        {
            // Given
            var base64Content = await ReadFileAsBase64(".\\Functions\\Import\\test.profile.json");

            m_BlobContentType = "application/json";
            m_FakeCloud.BlobClients[$"{m_PersonFileUploadQueueItem.ContainerName}/{m_PersonFileUploadQueueItem.BlobName}"] =
                CreateStorageBlobFromBase64StringContent(base64Content);

            // When
            await CreateFunction().Run(m_CloudQueueMessage, Mock.Of<ILogger>());

            // Then
            var container = m_FakeCosmos.PersonsContainer;
            container.Verify(c => c.CreateItemAsync(It.Is<Person>(p => p.Name == "Percy McProfile" &&
                                                                       p.ImportedLinkedInProfileUrl == "percy-mcprofile" &&
                                                                       p.ImportedLinkedInCompanyUrl == "https://www.linkedin.com/company/ikiru-people/" &&
                                                                       p.Location == "Alton, Hampshire, United Kingdom" &&
                                                                       p.JobTitle == "Just Backwards Of Square" &&
                                                                       p.Organisation == "Ikiru People, the global team behind FileFinder, GatedTalent, ISV Online &amp; Voyager Software"
                                                                       ),
                                                    It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
        }

        public static IEnumerable<object[]> LinkedInJsonPositionCases()
        {
            yield return new object[]
                         {
                             new Dictionary<string, object>[] { }, null, null, null
                         };
            yield return new object[]
                         {
                             new[]
                             {
                                 new Dictionary<string, object>
                                 {
                                     { "$type", "com.linkedin.voyager.dash.identity.profile.Position" },
                                     { "title", "title ended Dec" },
                                     { "companyName", "company ended Dec" },
                                     { "dateRange", new { start = new { month = 1, year = 2003 }, end = new { month = 12, year = 2006 } } },
                                     { "companyUrn", "abc-123-987" }
                                 },
                                 new Dictionary<string, object>
                                 {
                                     { "$type", "com.linkedin.voyager.dash.identity.profile.Position" },
                                     { "title", "title ended Nov" },
                                     { "companyName", "company ended Nov" },
                                     { "dateRange", new { start = new { month = 1, year = 2003 }, end = new { month = 11, year = 2006 } } }
                                 },
                                 new Dictionary<string, object>()
                                 {
                                     { "$type", "com.linkedin.voyager.dash.organization.Company" },
                                     { "entityUrn", "abc-123-987" },
                                     { "url", "https://www.linkedin.com/company/ended-dec-co" }
                                 }
                             },
                             "title ended Dec",
                             "company ended Dec",
                             "https://www.linkedin.com/company/ended-dec-co"
                         };
            yield return new object[]
                         {
                             new[]
                             {
                                 new Dictionary<string, object>
                                 {
                                     { "$type", "com.linkedin.voyager.dash.identity.profile.Position" },
                                     { "title", "Title Ended" },
                                     { "companyName", "Company Ended" },
                                     { "dateRange", new { start = new { month = 1, year = 2003 }, end = new { month = 12, year = 2006 } } }
                                 },
                                 new Dictionary<string, object>
                                 {
                                     { "$type", "com.linkedin.voyager.dash.identity.profile.Position" },
                                     { "title", "Title No End" },
                                     { "companyName", "Company No End" },
                                     { "dateRange", new { start = new { month = 1, year = 2003 } } }
                                 },
                                 new Dictionary<string, object>()
                                 {
                                     { "$type", "com.linkedin.voyager.dash.organization.Company" },
                                     { "entityUrn", "abc-123-987" },
                                     { "url", "https://www.linkedin.com/company/some-other-company" }
                                 }
                             },
                             "Title No End",
                             "Company No End",
                             null
                         };
            yield return new object[]
                         {
                             new[]
                             {
                                 new Dictionary<string, object>
                                 {
                                     { "$type", "com.linkedin.voyager.dash.identity.profile.Position" },
                                     { "title", "Title Ended" },
                                     { "companyName", "Company Ended" },
                                     { "dateRange", new { start = new { month = 1, year = 2003 }, end = new { month = 12, year = 2006 } } }
                                 },
                                 new Dictionary<string, object>
                                 {
                                     { "$type", "com.linkedin.voyager.dash.identity.profile.Position" },
                                     { "title", "Title No Start" },
                                     { "companyName", "Company No Start" },
                                     { "dateRange", new { end = new { month = 12, year = 2006 } } }
                                 }
                             },
                             "Title Ended",
                             "Company Ended",
                             null
                         };
            yield return new object[]
                         {
                             new[]
                             {
                                 new Dictionary<string, object>
                                 {
                                     { "$type", "com.linkedin.voyager.dash.identity.profile.Position" },
                                     { "title", "Title Started Jan 03" },
                                     { "companyName", "Company Started Jan 03" },
                                     { "dateRange", new { start = new { month = 1, year = 2003 } } }
                                 },
                                 new Dictionary<string, object>
                                 {
                                     { "$type", "com.linkedin.voyager.dash.identity.profile.Position" },
                                     { "title", "Title Started Feb 03" },
                                     { "companyName", "Company Started Feb 03" },
                                     { "dateRange", new { start = new { month = 2, year = 2003 } } },
                                     { "companyUrn", "abc-123-987" }
                                 },
                                 new Dictionary<string, object>()
                                 {
                                     { "$type", "com.linkedin.voyager.dash.organization.Company" },
                                     { "entityUrn", "zzz-123" },
                                     { "url", "https://www.linkedin.com/company/some-other-company" }
                                 }
                             },
                             "Title Started Feb 03",
                             "Company Started Feb 03",
                             null
                         };
            yield return new object[]
                         {
                             new[]
                             {
                                 new Dictionary<string, object>
                                 {
                                     { "$type", "com.linkedin.voyager.dash.identity.profile.Position" },
                                     { "title", "Current Role" },
                                     { "companyName", "Current Company" },
                                     { "dateRange", new { start = new { month = 1, year = 2020 } } },
                                     { "companyUrn", "UPPER-1"}
                                 },
                                 new Dictionary<string, object>()
                                 {
                                     { "$type", "com.linkedin.voyager.dash.organization.Company" },
                                     { "entityUrn", "upper-1" },
                                     { "url", "https://www.linkedin.com/company/matched-case-insensitive" }
                                 }
                             },
                             "Current Role",
                             "Current Company",
                             "https://www.linkedin.com/company/matched-case-insensitive"
                         };
            yield return new object[]
                         {
                             new[]
                             {
                                 new Dictionary<string, object>
                                 {
                                     { "$type", "com.linkedin.voyager.dash.identity.profile.Position" },
                                     { "title", "Title No Dates" },
                                     { "companyName", "Company No Dates" }
                                 },
                                 new Dictionary<string, object>
                                 {
                                     { "$type", "com.linkedin.voyager.dash.identity.profile.Position" },
                                     { "title", "Title Start Only" },
                                     { "companyName", "Company Start Only" },
                                     { "dateRange", new { start = new { month = 1, year = 2003 } } }
                                 },
                             },
                             "Title Start Only",
                             "Company Start Only",
                             null
                         };
        }

        [Theory]
        [MemberData(nameof(LinkedInJsonPositionCases))]
        public async Task FunctionCreatesItemsWithCorrectLatestJobDataFromJson(Dictionary<string, object>[] positionRawData, string expectedJobTitle, string expectedCompany, string expectedCompanyLinkedInUrl)
        {
            // Given
            var includedSectionData = new List<Dictionary<string, object>>(positionRawData)
                                      {
                                          new Dictionary<string, object>
                                          {
                                              { "$type", "com.linkedin.voyager.dash.identity.profile.Profile" },
                                              { "$recipeTypes", new [] { "com.linkedin.voyager.dash.deco.identity.profile.FullProfileWithEntities" }}
                                          }
                                      };

            var sourceJson = new
            {
                included = includedSectionData.ToArray()
            };
            var jsonText = JsonSerializer.Serialize(sourceJson);

            m_BlobContentType = "application/json";

            m_FakeCloud.BlobClients[$"{m_PersonFileUploadQueueItem.ContainerName}/{m_PersonFileUploadQueueItem.BlobName}"] =
                CreateStorageBlobFromBase64StringContent(CreateBase64ContentFromString(jsonText));

            // When 
            await CreateFunction().Run(m_CloudQueueMessage, Mock.Of<ILogger>());

            // Then
            var container = m_FakeCosmos.PersonsContainer;
            container.Verify(c => c.CreateItemAsync(It.Is<Person>(p => p.JobTitle == expectedJobTitle &&
                                                                       p.Organisation == expectedCompany &&
                                                                       p.ImportedLinkedInCompanyUrl == expectedCompanyLinkedInUrl
                                                                 ),
                                                    It.IsAny<PartitionKey?>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()));
        }

        #endregion

        [Fact]
        public async Task FunctionQueuesChangedLocationMessage()
        {
            // When
            await CreateFunction().Run(m_CloudQueueMessage, Mock.Of<ILogger>());


            // Then
            var queuedItem = m_FakeStorageQueue.GetQueuedItem<PersonLocationChangedQueueItem>(QueueStorage.QueueNames.PersonLocationChangedQueue);
            Assert.NotEqual(Guid.Empty, queuedItem.PersonId);
            Assert.Equal(m_BlobSearchFirmId, queuedItem.SearchFirmId);
        }

        [Fact]
        public async Task FunctionRecordsTelemetryForImport()
        {
            // Given
            var contentBytes = Encoding.UTF8.GetBytes("some blob content");
            var base64Content = Convert.ToBase64String(contentBytes);

            m_FakeCloud.BlobClients[$"{m_PersonFileUploadQueueItem.ContainerName}/{m_PersonFileUploadQueueItem.BlobName}"] =
                CreateStorageBlobFromBase64StringContent(base64Content);


            // When
            await CreateFunction().Run(m_CloudQueueMessage, Mock.Of<ILogger>());

            // Then
            Assert.Equal(1, m_Telemetry.ReceivedTelemetry.Count);
            var telemetry = (EventTelemetry)m_Telemetry.ReceivedTelemetry.Single();
            Assert.Equal("ImportFunction.ProfileImported", telemetry.Name);
            var props = telemetry.Properties;
            Assert.True(props.ContainsKey("BlobSize"));
            Assert.Equal(contentBytes.Length.ToString(), props["BlobSize"]);
            Assert.True(props.ContainsKey("ImportId"));
            Assert.Equal(m_BlobImportId.ToString(), props["ImportId"]);
            Assert.True(props.ContainsKey("ContentType"));
            Assert.Equal(m_BlobContentType, props["ContentType"]);
        }

        #region Private Helpers

        private ImportFunction CreateFunction()
        {
            m_ParseResponse.Value.ParsedDocument = JsonSerializer.Serialize(m_SovrenParsedDocument);
            return new FunctionBuilder<ImportFunction>()
                  .SetFakeCosmos(m_FakeCosmos)
                  .SetFakeCloud(m_FakeCloud)
                  .AddTransient(m_ParsingApi.Object)
                  .AddTransient(m_Telemetry.Config)
                  .AddTransient(m_FakeStorageQueue.QueueServiceClient.Object)
                  .Build();
        }

        private static SovrenParsedDocument CreateFullParsedDocumentResult()
        {
            return new SovrenParsedDocument
            {
                Resume = new Resume
                {
                    StructuredXMLResume = new Structuredxmlresume
                    {
                        ContactInfo = new Contactinfo
                        {
                            PersonName = new Personname
                            {
                                FormattedName = ""
                            },
                            ContactMethod = new[]
                                            {
                                                new Contactmethod
                                                {
                                                    Use = "linkedIn",
                                                    InternetWebAddress = ""
                                                },
                                                new Contactmethod
                                                {
                                                    InternetEmailAddress = ""
                                                },
                                                new Contactmethod
                                                {
                                                    PostalAddress = new Postaladdress
                                                                    {
                                                                        CountryCode = "",
                                                                        Municipality = "",
                                                                        Region = new []
                                                                                 {
                                                                                    ""
                                                                                 }
                                                                    }
                                                },
                                                new Contactmethod
                                                {
                                                    Telephone = new PhoneNumber
                                                                {
                                                                    FormattedNumber = ""
                                                                }
                                                },
                                                new Contactmethod
                                                {
                                                    Mobile = new PhoneNumber
                                                                {
                                                                    FormattedNumber = ""
                                                                }
                                                }
                                            }

                        },
                        EmploymentHistory = new Employmenthistory
                        {
                            EmployerOrg = new[]
                                           {
                                               new Employerorg
                                               {
                                                   EmployerOrgName = "",
                                                   PositionHistory = new []
                                                                     {
                                                                         new Positionhistory
                                                                         {
                                                                             Title = ""
                                                                         },
                                                                     }
                                               },
                                               new Employerorg
                                               {
                                                   EmployerOrgName = "",
                                                   PositionHistory = new []
                                                                     {
                                                                         new Positionhistory
                                                                         {
                                                                             Title = ""
                                                                         },
                                                                     }
                                               }
                                           }
                        }

                    }
                }
            };
        }

        private static async Task<string> ReadFileAsBase64(string filePath)
        {
            var contentBytes = await File.ReadAllBytesAsync(filePath);
            return Convert.ToBase64String(contentBytes);
        }

        private static string CreateBase64ContentFromString(string utf8Content)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(utf8Content));
        }

        // ReSharper disable once StringLiteralTypo
        private static readonly Uri s_BaseStorageUri = new Uri("http://somestorage/devstoreaccount1/imports/");

        private Mock<BlobClient> CreateDefaultStorageBlob() => CreateStorageBlobFromBase64StringContent("");

        private Mock<BlobClient> CreateStorageBlobFromBase64StringContent(string base64String)
        {
            var b64Bytes = Convert.FromBase64String(base64String);

            var blobInfoMetaData = new Dictionary<string, string>(new List<KeyValuePair<string, string>>()
                                                                  { new KeyValuePair<string, string>("FileName", m_BlobLinkedInProfileUrl) });

            var blobDownloadInfo = BlobsModelFactory.BlobDownloadInfo(metadata: blobInfoMetaData, contentType: m_BlobContentType,
                                                                               content: new MemoryStream(b64Bytes));

            var blobProperties = BlobsModelFactory.BlobProperties(metadata: blobInfoMetaData, contentType: m_BlobContentType);

            var blob = new Mock<BlobClient>();
            blob.Setup(x => x.Download())
                .Returns(() => Mock.Of<Azure.Response<BlobDownloadInfo>>(r => r.Value == blobDownloadInfo));

            blob.Setup(x => x.GetPropertiesAsync(It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
                .Returns<BlobRequestConditions, CancellationToken>(
                                                    (b, c) => Task.FromResult(Mock.Of<Azure.Response<BlobProperties>>(r => r.Value == blobProperties)));
            blob.SetupGet(b => b.Uri)
                .Returns(new Uri(s_BaseStorageUri + "/" + $"{m_BlobSearchFirmId}/{m_BlobImportId}"));

            return blob;
        }

        #endregion
    }
}
