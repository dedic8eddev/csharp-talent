using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Ikiru.Parsnips.IntegrationTests.Helpers;
using Ikiru.Parsnips.IntegrationTests.Helpers.Data;
using Ikiru.Parsnips.IntegrationTests.Helpers.HttpMessages;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Blobs;
using Xunit;
using Xunit.Abstractions;

namespace Ikiru.Parsnips.IntegrationTests.Features.Api.Persons.Documents
{
    [Collection(nameof(IntegrationTestCollection))]
    public class DocumentsTests : IntegrationTestBase, IClassFixture<DocumentsTests.DocumentsTestsClassFixture>
    {
        private readonly DocumentsTestsClassFixture m_ClassFixture;
        private static readonly string m_FileName = "mydoc.docx";

        public DocumentsTests(IntegrationTestFixture fixture, DocumentsTestsClassFixture classFixture, ITestOutputHelper output) : base(fixture, output)
        {
            m_ClassFixture = classFixture;
        }

        public sealed class DocumentsTestsClassFixture : IDisposable
        {
            public IntTestServer Server { get; }

            // ReSharper disable once UnusedParameter.Local
            public DocumentsTestsClassFixture()
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

        private async Task AddPersonIntoCosmos()
        {
            m_Person = new Person(m_ClassFixture.Server.Authentication.DefaultSearchFirmId)
                       {
                           Name = "Person for Notes Tests"
                       };

            m_Person = await m_ClassFixture.Server.InsertItemIntoCosmos(TestDataManipulator.PersonsContainerName, m_Person.SearchFirmId, m_Person);
        }

        [Fact]
        public async Task PostShouldRespondWithOkAndFileUploadDetails()
        {
            // GIVEN
            await AddPersonIntoCosmos();
            var content = CreateMultipartFileUploadContent();

            // WHEN
            var response = await m_ClassFixture.Server.Client.PostAsync($"/api/persons/{m_Person.Id}/documents", content);

            // Then
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);

            var r = new
                    {
                        Id = Guid.Empty,
                        Filename = "",
                        CreatedDate = DateTimeOffset.MinValue
                    };

            var responseJson = await response.Content.DeserializeToAnonymousType(r);
            
            Assert.NotEqual(Guid.Empty, responseJson.Id);
            Assert.Equal(m_FileName, responseJson.Filename);
            Assert.Equal(DateTimeOffset.UtcNow.Date, responseJson.CreatedDate.Date);
        }

        [Fact]
        public async Task GetListShouldRespondWithResults()
        {
            // Given
            await AddPersonIntoCosmos();

            var postResponse = await m_ClassFixture.Server.Client.PostAsync($"/api/persons/{m_Person.Id}/documents", CreateMultipartFileUploadContent());
            Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);
            var createdDocument = await postResponse.Content.DeserializeToAnonymousType(new { Id = Guid.Empty, Filename = "", CreatedDate = DateTimeOffset.MinValue });

            // When
            var response = await m_ClassFixture.Server.Client.GetAsync($"/api/persons/{m_Person.Id}/documents");

            // Then
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);

            var r = new
                    {
                        Documents  = new []
                                     {
                                         new
                                         {
                                             Id = Guid.Empty,
                                             Filename = "",
                                             CreatedDate = DateTimeOffset.MinValue
                                         }
                                     }
                    };

            var responseJson = await response.Content.DeserializeToAnonymousType(r);
            Assert.NotNull(responseJson);
            Assert.NotNull(responseJson.Documents);
            var noteResult = Assert.Single(responseJson.Documents);

            Assert.Equal(createdDocument.Id, noteResult.Id);
            Assert.Equal(createdDocument.Filename, noteResult.Filename);
            Assert.Equal(createdDocument.CreatedDate, noteResult.CreatedDate);
        }

        [Fact]
        public async Task GetDownloadShouldRespondWithOkResult()
        {
            // Given
            await AddPersonIntoCosmos();

            var postResponse = await m_ClassFixture.Server.Client.PostAsync($"/api/persons/{m_Person.Id}/documents", CreateMultipartFileUploadContent());
            Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);
            var createdDocument = await postResponse.Content.DeserializeToAnonymousType(new { Id = Guid.Empty, Filename = "", CreatedDate = DateTimeOffset.MinValue });

            // When
            var response = await m_ClassFixture.Server.Client.GetAsync($"/api/persons/{m_Person.Id}/documents/{createdDocument.Id}/download");

            // Then
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);

            var r = new
                    {
                        TemporaryUrl = ""
                    };
            var responseJson = await response.Content.DeserializeToAnonymousType(r);
            Assert.NotNull(responseJson);

            Assert.StartsWith($"http://127.0.0.1:10000/devstoreaccount1/{BlobStorage.ContainerNames.PersonsDocuments}/{m_Person.SearchFirmId}/{m_Person.Id}/{createdDocument.Id}?", responseJson.TemporaryUrl);
        }

        private static MultipartFormDataContent CreateMultipartFileUploadContent()
        {
            var multipartFormDataContent = new MultipartFormDataContent();
            var byteArrayContent = new ByteArrayContent(Encoding.UTF8.GetBytes("This is a test CV to upload for integration tests."));
            byteArrayContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
            multipartFormDataContent.Add(byteArrayContent, "file", m_FileName);

            return multipartFormDataContent;
        }
    }
}