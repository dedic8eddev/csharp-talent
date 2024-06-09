using System;
using System.Net;
using System.Threading.Tasks;
using Ikiru.Parsnips.IntegrationTests.Helpers;
using Ikiru.Parsnips.IntegrationTests.Helpers.Data;
using Ikiru.Parsnips.IntegrationTests.Helpers.HttpMessages;
using Ikiru.Parsnips.Domain;
using Xunit;
using Xunit.Abstractions;

namespace Ikiru.Parsnips.IntegrationTests.Features.Api.Persons.Notes
{
    [Collection(nameof(IntegrationTestCollection))]
    public class NotesTests : IntegrationTestBase, IClassFixture<NotesTests.NotesTestsClassFixture>
    {
        private readonly NotesTestsClassFixture m_ClassFixture;
        private Person m_Person;
        private Assignment m_Assignment;

        public NotesTests(IntegrationTestFixture fixture, NotesTestsClassFixture classFixture, ITestOutputHelper output) : base(fixture, output)
        {
            m_ClassFixture = classFixture;
        }

        public class NotesTestsClassFixture : IDisposable
        {
            public IntTestServer Server { get; }

            // ReSharper disable once UnusedParameter.Local
            public NotesTestsClassFixture(IntegrationTestFixture fixture)
            {
                Server = new TestServerBuilder()
                   .Build();
            }

            public void Dispose()
            {
                Server.Dispose();
            }
        }

        private async Task EnsureDependentItemsInCosmos()
        {
            m_Person = new Person(m_ClassFixture.Server.Authentication.DefaultSearchFirmId)
            {
                Name = "Person for Notes Tests"
            };

            m_Person = await m_ClassFixture.Server.InsertItemIntoCosmos(TestDataManipulator.PersonsContainerName, m_Person.SearchFirmId, m_Person);

            m_Assignment = new Assignment(m_ClassFixture.Server.Authentication.DefaultSearchFirmId)
            {
                Name = "Assignment for Person Notes Tests v1"
            };

            m_Assignment = await m_ClassFixture.Server.AddUniqueItemIntoCosmos(TestDataManipulator.AssignmentsContainerName, m_Assignment.SearchFirmId, a => a.Name == m_Assignment.Name, m_Assignment);
        }

        [Fact]
        public async Task PostShouldRespondWithOk()
        {
            // Given
            await EnsureDependentItemsInCosmos();
            var command = new
            {
                NoteTitle = "a note title about a person",
                NoteDescription = "some notes about the persons activity",
                AssignmentId = m_Assignment.Id
            };

            // When
            var response = await m_ClassFixture.Server.Client.PostAsync($"/api/persons/{m_Person.Id}/notes", new JsonContent(command));

            // Then
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var r = new
            {
                Id = Guid.Empty,
                PersonId = Guid.Empty,
                NoteTitle = "",
                NoteDescription = "",
                AssignmentId = (Guid?)null,
                CreatedDate = DateTimeOffset.MinValue,
                CreatedByUserId = Guid.Empty,
                LinkCreatedByUser = new
                                    {
                                        Id = Guid.Empty,
                                        FirstName = "",
                                        LastName = ""
                                    },
                UpdatedByUserId = Guid.Empty,
                LinkUpdatedByUser =  new
                                     {
                                         Id = Guid.Empty,
                                         FirstName = "",
                                         LastName = ""
                                     },
                LinkAssignment = new
                                 {
                                     Id = Guid.Empty,
                                     Name = ""
                                 }
            };

            var responseJson = await response.Content.DeserializeToAnonymousType(r);

            Assert.NotEqual(Guid.Empty, responseJson.Id);
            Assert.Equal(m_Person.Id, responseJson.PersonId);
            Assert.Equal(command.NoteTitle, responseJson.NoteTitle);
            Assert.Equal(command.NoteDescription, responseJson.NoteDescription);
            Assert.Equal(DateTimeOffset.UtcNow.Date, responseJson.CreatedDate.Date);
            Assert.Equal(m_ClassFixture.Server.Authentication.DefaultUser.Id, responseJson.CreatedByUserId);
            Assert.NotNull(responseJson.LinkCreatedByUser);
            Assert.Equal(m_ClassFixture.Server.Authentication.DefaultUser.Id, responseJson.LinkCreatedByUser.Id);
            Assert.Equal(m_ClassFixture.Server.Authentication.DefaultUser.FirstName, responseJson.LinkCreatedByUser.FirstName);
            Assert.Equal(m_ClassFixture.Server.Authentication.DefaultUser.LastName, responseJson.LinkCreatedByUser.LastName);
            Assert.Equal(Guid.Empty, responseJson.UpdatedByUserId);
            Assert.Null(responseJson.LinkUpdatedByUser);
            Assert.NotNull(responseJson.LinkAssignment);
            Assert.Equal(m_Assignment.Id, responseJson.LinkAssignment.Id);
            Assert.Equal(m_Assignment.Name, responseJson.LinkAssignment.Name);
        }

        [Fact]
        public async Task GetListShouldRespondWithResults()
        {
            // Given
            await EnsureDependentItemsInCosmos();

            var postCommand = new
            {
                NoteTitle = "Note Title for Get List",
                NoteDescription = "Note Description for Get List",
                AssignmentId = m_Assignment.Id
            };
            var postResponse = await m_ClassFixture.Server.Client.PostAsync($"/api/persons/{m_Person.Id}/notes", new JsonContent(postCommand));
            Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);
            var createdNote = await postResponse.Content.DeserializeToAnonymousType(new { Id = Guid.Empty, NoteTitle = "", NoteDescription = "" });
            
            var putCommand = new
                             {
                                 AssignmentId = m_Assignment.Id,
                                 NoteTitle = "Updated Note Title for Get List",
                                 NoteDescription = "Update Note Description for Get List",
                             };

            // When
            var putResponse = await m_ClassFixture.Server.Client.PutAsync($"/api/persons/{m_Person.Id}/notes/{createdNote.Id}", new JsonContent(putCommand));
            Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);
            var updatedNote = await putResponse.Content.DeserializeToAnonymousType(new { Id = Guid.Empty, NoteTitle = "", NoteDescription = "",
                                                                                           LinkUpdatedByUser = new {
                                                                                               Id = Guid.Empty,
                                                                                               FirstName = "",
                                                                                               LastName = ""
                                                                                       }});

            // When
            var response = await m_ClassFixture.Server.Client.GetAsync($"/api/persons/{m_Person.Id}/notes?expand=createdByUser,updatedByUser,assignment&limit=1");

            // Then
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var r = new
            {
                HasMoreResults = false,
                Notes = new[]
                                {
                                    new
                                    {
                                        Id = Guid.Empty,
                                        PersonId = Guid.Empty,
                                        NoteTitle = "",
                                        NoteDescription = "",
                                        AssignmentId = (Guid?)null,
                                        CreatedDate = DateTimeOffset.MinValue,
                                        CreatedByUserId = Guid.Empty,
                                        LinkCreatedByUser = new
                                                            {
                                                                Id = Guid.Empty,
                                                                FirstName = "",
                                                                LastName = ""
                                                            },
                                        UpdatedByUserId = Guid.Empty,
                                        LinkUpdatedByUser =  new
                                                             {
                                                                 Id = Guid.Empty,
                                                                 FirstName = "",
                                                                 LastName = ""
                                                             },
                                        LinkAssignment = new
                                                         {
                                                             Id = Guid.Empty,
                                                             Name = ""
                                                         }
                                    }
                                }
            };

            var responseJson = await response.Content.DeserializeToAnonymousType(r);
            Assert.NotNull(responseJson);
            Assert.NotNull(responseJson.Notes);
            Assert.False(responseJson.HasMoreResults);
            var noteResult = Assert.Single(responseJson.Notes);

            Assert.Equal(createdNote.Id, noteResult.Id);
            Assert.Equal(m_Person.Id, noteResult.PersonId);

            Assert.Equal(updatedNote.NoteTitle, noteResult.NoteTitle);
            Assert.Equal(updatedNote.NoteDescription, noteResult.NoteDescription);

            Assert.Equal(m_Assignment.Id, noteResult.AssignmentId);
            Assert.Equal(DateTimeOffset.UtcNow.Date, noteResult.CreatedDate.Date);
            Assert.Equal(m_ClassFixture.Server.Authentication.DefaultUser.Id, noteResult.CreatedByUserId);
            Assert.NotNull(noteResult.LinkCreatedByUser);
            Assert.Equal(m_ClassFixture.Server.Authentication.DefaultUser.Id, noteResult.LinkCreatedByUser.Id);
            Assert.Equal(m_ClassFixture.Server.Authentication.DefaultUser.FirstName, noteResult.LinkCreatedByUser.FirstName);
            Assert.NotNull(noteResult.LinkAssignment);
            Assert.Equal(m_Assignment.Id, noteResult.LinkAssignment.Id);
            Assert.Equal(m_Assignment.Name, noteResult.LinkAssignment.Name);

            Assert.Equal(updatedNote.NoteTitle, noteResult.NoteTitle);
            Assert.Equal(updatedNote.NoteDescription, noteResult.NoteDescription);
            Assert.Equal(updatedNote.LinkUpdatedByUser.Id, noteResult.LinkUpdatedByUser.Id);
            Assert.Equal(updatedNote.LinkUpdatedByUser.FirstName, noteResult.LinkUpdatedByUser.FirstName);
            Assert.Equal(updatedNote.LinkUpdatedByUser.LastName, noteResult.LinkUpdatedByUser.LastName);
        }

        [Fact]
        public async Task PutShouldUpdateAndReturnOk()
        {
            // Given
            await EnsureDependentItemsInCosmos();

            var postCommand = new
            {
                NoteTitle = "Note Title for Get List",
                NoteDescription = "Note Description for Get List",
                AssignmentId = m_Assignment.Id
            };

            var postResponse = await m_ClassFixture.Server.Client.PostAsync($"/api/persons/{m_Person.Id}/notes", new JsonContent(postCommand));
            Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);
            var createdNote = await postResponse.Content.DeserializeToAnonymousType(new { Id = Guid.Empty, NoteTitle = "", NoteDescription = "" });

            var putCommand = new
            {
                AssignmentId = m_Assignment.Id,
                NoteTitle = "Updated Note Title for Get List",
                NoteDescription = "Update Note Description for Get List",
            };

            // When
            var response = await m_ClassFixture.Server.Client.PutAsync($"/api/persons/{m_Person.Id}/notes/{createdNote.Id}", new JsonContent(putCommand));

            var r = new
                    {
                        Id = Guid.Empty,
                        NoteTitle = "",
                        NoteDescription = "",
                        PersonId = Guid.Empty,
                        UpdatedDate = DateTimeOffset.MinValue,
                        LinkAssignment = new
                                         {
                                             Id = Guid.Empty,
                                             Name = ""
                                         },
                        LinkUpdatedByUser = new
                                            {
                                                Id = Guid.Empty,
                                                FirstName ="",
                                                LastName = ""
                                            },
                        AssignmentId = Guid.Empty
                    };

            var responseJson = await response.Content.DeserializeToAnonymousType(r);

            Assert.NotEqual(Guid.Empty, responseJson.Id);
            Assert.Equal(m_Person.Id, responseJson.PersonId);
            Assert.Equal(putCommand.NoteTitle, responseJson.NoteTitle);
            Assert.Equal(putCommand.NoteDescription, responseJson.NoteDescription);
            Assert.Equal(DateTimeOffset.UtcNow.Date, responseJson.UpdatedDate.Date);
            Assert.Equal(m_ClassFixture.Server.Authentication.DefaultUser.Id, responseJson.LinkUpdatedByUser.Id);
            Assert.NotNull(responseJson.LinkUpdatedByUser);
            Assert.Equal(m_ClassFixture.Server.Authentication.DefaultUser.Id, responseJson.LinkUpdatedByUser.Id);
            Assert.Equal(m_ClassFixture.Server.Authentication.DefaultUser.FirstName, responseJson.LinkUpdatedByUser.FirstName);
            Assert.Equal(m_ClassFixture.Server.Authentication.DefaultUser.LastName, responseJson.LinkUpdatedByUser.LastName);
            Assert.NotNull(responseJson.LinkAssignment);
            Assert.Equal(m_Assignment.Id, responseJson.LinkAssignment.Id);
            Assert.Equal(m_Assignment.Name, responseJson.LinkAssignment.Name);
        }
    }
}
