using Ikiru.Parsnips.Api;
using Ikiru.Parsnips.Api.Controllers.Assignments.Notes;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.IntegrationTests.Helpers;
using Ikiru.Parsnips.IntegrationTests.Helpers.HttpMessages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Mvc;
using Xunit;
using Xunit.Abstractions;

namespace Ikiru.Parsnips.IntegrationTests.Features.Api.Assignments.Notes
{
    [Collection(nameof(IntegrationTestCollection))]
    public class AssignmentNotesTests : IntegrationTestBase, IClassFixture<AssignmentNotesTests.AssignmentNotesTestsClassFixture>
    {
        private readonly AssignmentNotesTestsClassFixture m_ClassFixture;

        public AssignmentNotesTests(IntegrationTestFixture fixture, AssignmentNotesTestsClassFixture classFixture, ITestOutputHelper output) : base(fixture, output)
        {
            m_ClassFixture = classFixture;
        }

        public sealed class AssignmentNotesTestsClassFixture : IDisposable
        {
            public IntTestServer Server { get; }

            // ReSharper disable once UnusedParameter.Local
            public AssignmentNotesTestsClassFixture()
            {
                Server = new TestServerBuilder()
                   .Build();
            }

            public void Dispose()
            {
                Server.Dispose();
            }
        }




        [Fact]
        public async Task PostCreateAssignmentNote()
        {
            // Create assignment
            var createAssignmentCommand = new
            {
                Name = "assignmentName",
                CompanyName = "CompanyName",
                JobTitle = "jobTitle",
                Location = "Location",
                StartDate = DateTimeOffset.Now
            };
            var r = new
            {
                Id = Guid.NewGuid(),
                Status = AssignmentStatus.Active
            };
            // var assignmentReponse = await m_ClassFixture.Server.Client.PostAsync("/api/assignments", new JsonContent(createAssignmentCommand));
            // var createdAssignment = JsonConvert.DeserializeAnonymousType(await assignmentReponse.Content.ReadAsStringAsync(), r);
            var createdAssignment = await MakeHttpRequest("/api/assignments", HttpVerbs.Post, HttpStatusCode.Created, createAssignmentCommand, r);





            // Create note for assignment
            var createAssignmentNoteCommand = new
            {
                Title = "note title",
                Description = "note description",
                CreatedDate = DateTimeOffset.Now
            };


            var r2 = new
            {
                updatedBy = new { },
                createdBy = new { },
                note = new
                {
                    noteTitle = "",
                    noteDescription = "",
                    assignmentId = Guid.Empty,
                    createdBy = Guid.Empty,
                    updatedBy = (Guid?)Guid.Empty,
                    updateDate = (DateTimeOffset?)DateTimeOffset.MinValue,
                    createdDate = DateTimeOffset.MinValue
                }
            };



            var createdAssignmentNoteResponse = await m_ClassFixture.Server.Client.PostAsync($"/api/assignments/{createdAssignment.Id}/notes", new JsonContent(createAssignmentNoteCommand));

            var createdAssignmentNote = JsonConvert.DeserializeAnonymousType(await createdAssignmentNoteResponse.Content.ReadAsStringAsync(), r2);

            Assert.Equal(createAssignmentNoteCommand.Title, createdAssignmentNote.note.noteTitle);
            Assert.Equal(createAssignmentNoteCommand.Description, createdAssignmentNote.note.noteDescription);
            Assert.Equal(createAssignmentNoteCommand.CreatedDate.Date, createdAssignmentNote.note.createdDate.Date);
            Assert.NotEqual(Guid.Empty, createdAssignmentNote.note.createdBy);
            Assert.Equal(createdAssignment.Id, createdAssignmentNote.note.assignmentId);
        }

        [Fact]
        public async Task UpdateCreateAssignmentNote()
        {

            // Create assignment
            var createAssignmentCommand = new
            {
                Name = "assignmentName",
                CompanyName = "CompanyName",
                JobTitle = "jobTitle",
                Location = "Location",
                StartDate = DateTimeOffset.Now
            };

            var assignmentReponse = await m_ClassFixture.Server.Client.PostAsync("/api/assignments", new JsonContent(createAssignmentCommand));

            var r = new
            {
                id = Guid.NewGuid(),
                status = AssignmentStatus.Active
            };

            var createdAssignment = JsonConvert.DeserializeAnonymousType(await assignmentReponse.Content.ReadAsStringAsync(), r);

            // Create note for assignment
            var createAssignmentNoteCommand = new
            {
                Title = "note title",
                Description = "note description",
                CreatedDate = DateTimeOffset.Now
            };

            var createdAssignmentNoteResponse = await m_ClassFixture.Server.Client.PostAsync($"/api/assignments/{createdAssignment.id}/notes", new JsonContent(createAssignmentNoteCommand));

            var r2 = new
            {
                updatedBy = new { },
                createdBy = new { },
                note = new
                {
                    id = Guid.Empty,
                    noteTitle = "",
                    noteDescription = "",
                    assignmentId = Guid.Empty,
                    createdBy = Guid.Empty,
                    updatedBy = (Guid?)Guid.Empty,
                    updateDate = (DateTimeOffset?)DateTimeOffset.MinValue,
                    createdDate = DateTimeOffset.MinValue
                }
            };

            var createdAssignmentNote = JsonConvert.DeserializeAnonymousType(await createdAssignmentNoteResponse.Content.ReadAsStringAsync(), r2);


            // Update note for assignment
            var updateAssignmentNoteCommand = new
            {
                Title = "updatenote title",
                Description = "update note description",
                UpdatedDate = DateTimeOffset.Now
            };


            var r3 = new
            {
                updatedBy = new { },
                createdBy = new { },
                note = new
                {
                    noteTitle = "",
                    noteDescription = "",
                    assignmentId = Guid.Empty,
                    createdBy = Guid.Empty,
                    updatedBy = (Guid?)Guid.Empty,
                    updatedDate = (DateTimeOffset?)DateTimeOffset.MinValue,
                    createdDate = DateTimeOffset.MinValue
                }
            };


            var updatedAssignmentNoteResponse = await m_ClassFixture.Server.Client.PutAsync($"/api/assignments/{createdAssignment.id}/notes/{createdAssignmentNote.note.id}", new JsonContent(updateAssignmentNoteCommand));
            Assert.Equal(HttpStatusCode.OK, updatedAssignmentNoteResponse.StatusCode);

            var updatedAssignmentNote = JsonConvert.DeserializeAnonymousType(await updatedAssignmentNoteResponse.Content.ReadAsStringAsync(), r3);

            Assert.Equal(updateAssignmentNoteCommand.Title, updatedAssignmentNote.note.noteTitle);
            Assert.Equal(updateAssignmentNoteCommand.Description, updatedAssignmentNote.note.noteDescription);
            Assert.Equal(updateAssignmentNoteCommand.UpdatedDate.Date, updatedAssignmentNote.note.updatedDate.Value.Date);
            Assert.NotEqual(Guid.Empty, updatedAssignmentNote.note.updatedBy);
            Assert.Equal(createdAssignment.id, updatedAssignmentNote.note.assignmentId);
        }

        [Fact]
        public async Task GetCreatedAssignmentNotes()
        {
            // Create assignment
            var createAssignmentCommand = new
            {
                Name = "assignmentName",
                CompanyName = "CompanyName",
                JobTitle = "jobTitle",
                Location = "Location",
                StartDate = DateTimeOffset.Now
            };

            var assignmentReponse = await m_ClassFixture.Server.Client.PostAsync("/api/assignments", new JsonContent(createAssignmentCommand));

            var r = new
            {
                id = Guid.NewGuid(),
                status = AssignmentStatus.Active
            };

            var createdAssignment = JsonConvert.DeserializeAnonymousType(await assignmentReponse.Content.ReadAsStringAsync(), r);

            // Create note for assignment
            var createAssignmentNoteCommand1 = new
            {
                Title = "note title",
                Description = "note description",
                CreatedDate = DateTimeOffset.Now
            };

            var createAssignmentNoteCommand2 = new
            {
                Title = "note title2",
                Description = "note description2",
                CreatedDate = DateTimeOffset.Now
            };

            var r2 = new
            {
                updatedBy = new { },
                createdBy = new { },
                note = new
                {
                    id = Guid.Empty,
                    noteTitle = "",
                    noteDescription = "",
                    assignmentId = Guid.Empty,
                    createdBy = Guid.Empty,
                    updatedBy = (Guid?)Guid.Empty,
                    updateDate = (DateTimeOffset?)DateTimeOffset.MinValue,
                    createdDate = DateTimeOffset.MinValue
                }
            };

            var createdAssignmentNotesResponse1 = await m_ClassFixture.Server.Client.PostAsync($"/api/assignments/{createdAssignment.id}/notes", new JsonContent(createAssignmentNoteCommand1));
            var createdAssignmentNotesResponse2 = await m_ClassFixture.Server.Client.PostAsync($"/api/assignments/{createdAssignment.id}/notes", new JsonContent(createAssignmentNoteCommand2));

            var createdAssignmentNotes1 = JsonConvert.DeserializeAnonymousType(await createdAssignmentNotesResponse1.Content.ReadAsStringAsync(), r2);
            var createdAssignmentNotes2 = JsonConvert.DeserializeAnonymousType(await createdAssignmentNotesResponse2.Content.ReadAsStringAsync(), r2);


            // Get All notes for assignment

            var r3 = new[] { new
            {
                updatedBy = new { },
                createdBy = new { },
                note = new
                {
                    id = Guid.Empty,
                    noteTitle = "",
                    noteDescription = "",
                    assignmentId = Guid.Empty,
                    createdBy = Guid.Empty,
                    updatedBy = (Guid?)Guid.Empty,
                    updateDate = (DateTimeOffset?)DateTimeOffset.MinValue,
                    createdDate = DateTimeOffset.MinValue
                }
            }};
            var getAllAssignmentNotesResponse = await m_ClassFixture.Server.Client.GetAsync($"/api/assignments/{createdAssignment.id}/notes");

            var allCreatedNotesForAssignment = JsonConvert.DeserializeAnonymousType(await getAllAssignmentNotesResponse.Content.ReadAsStringAsync(), r3);

            Assert.Equal(2, allCreatedNotesForAssignment.Length);
            Assert.Equal(allCreatedNotesForAssignment[1].note.id, createdAssignmentNotes1.note.id);
            Assert.Equal(allCreatedNotesForAssignment[1].note.noteTitle, createdAssignmentNotes1.note.noteTitle);
            Assert.Equal(allCreatedNotesForAssignment[0].note.id, createdAssignmentNotes2.note.id);
            Assert.Equal(allCreatedNotesForAssignment[0].note.noteTitle, createdAssignmentNotes2.note.noteTitle);


        }



        private async Task<TOut> MakeHttpRequest<TIn, TOut>(string requestUriRoute, HttpVerbs verb, HttpStatusCode statusCode, TIn request, TOut response)
        {
            HttpResponseMessage httpResponse = new HttpResponseMessage();

            switch (verb)
            {
                case HttpVerbs.Post:
                    httpResponse = await m_ClassFixture.Server.Client.PostAsync(requestUriRoute, new JsonContent(request));
                    break;
                case HttpVerbs.Get:
                    httpResponse = await m_ClassFixture.Server.Client.GetAsync(requestUriRoute);
                    break;
                case HttpVerbs.Put:
                    httpResponse = await m_ClassFixture.Server.Client.PostAsync(requestUriRoute, new JsonContent(request));
                    break;
                case HttpVerbs.Delete:
                    httpResponse = await m_ClassFixture.Server.Client.PostAsync(requestUriRoute, new JsonContent(request));
                    break;
                case HttpVerbs.Head:
                    break;
                case HttpVerbs.Patch:
                    break;
                case HttpVerbs.Options:
                    break;
                default:
                    break;
            }

            Assert.Equal(statusCode, httpResponse.StatusCode);

            return JsonConvert.DeserializeAnonymousType(await httpResponse.Content.ReadAsStringAsync(), response);
        }

    }
}
