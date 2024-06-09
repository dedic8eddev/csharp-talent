using Ikiru.Parsnips.Api.Controllers.Persons.Notes;
using Ikiru.Parsnips.Api.ModelBinding;
using Ikiru.Parsnips.UnitTests.Helpers;
using Ikiru.Parsnips.Domain;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Persons.Notes
{
    public class GetListTests
    {
        private readonly Guid m_SearchFirmId = Guid.NewGuid();

        private readonly Person m_StoredPerson;
        private readonly List<SearchFirmUser> m_StoredUsers;
        private readonly List<Assignment> m_StoredAssignments;
        private readonly List<Note> m_StoredNotes;

        private readonly Guid m_MissingPersonId = Guid.NewGuid();

        private readonly GetList.Query m_Query = new GetList.Query();

        private readonly FakeCosmos m_FakeCosmos = new FakeCosmos();

        public GetListTests()
        {
            m_StoredPerson = new Person(m_SearchFirmId)
            {
                Name = "Parent Person whose Notes these are"
            };

            m_StoredUsers = new List<SearchFirmUser>
                            {
                                new SearchFirmUser(m_SearchFirmId)
                                {
                                    FirstName = "User One FirstName",
                                    LastName = "User One LastName"
                                },
                                new SearchFirmUser(m_SearchFirmId)
                                {
                                    FirstName = "User Two FirstName",
                                    LastName = "User Two LastName"
                                }
                            };

            m_StoredAssignments = new List<Assignment>(1)
                                  {
                                      new Assignment(m_SearchFirmId)
                                      {
                                          Name = "Assign One"
                                      }
                                  };

            m_StoredNotes = new List<Note>(4)
                            {
                                new Note(m_StoredPerson.Id, m_StoredUsers[0].Id, m_SearchFirmId)
                                {
                                    NoteTitle = "Note One",
                                    NoteDescription = "Note One Desc"
                                },
                                new Note(Guid.NewGuid(), m_StoredUsers[0].Id, m_SearchFirmId)
                                {
                                    NoteTitle = "Note Two",
                                    NoteDescription = "Note Two Desc [for a different Person]"
                                },
                                new Note(m_StoredPerson.Id, m_StoredUsers[0].Id, m_SearchFirmId)
                                {
                                    NoteTitle = "Note Three",
                                    NoteDescription = "Note Three Desc",
                                    AssignmentId = m_StoredAssignments[0].Id
                                },
                                new Note(m_StoredPerson.Id, m_StoredUsers[0].Id, m_SearchFirmId)
                                {
                                    NoteTitle = "Updated Four Three",
                                    NoteDescription = "Updated Note Four Desc",
                                    AssignmentId = m_StoredAssignments[0].Id,
                                    UpdatedDate = DateTimeOffset.Now,
                                    UpdatedBy = m_StoredUsers[1].Id

                                }
                            };

            m_FakeCosmos.EnableContainerLinqQuery(FakeCosmos.PersonNotesContainerName, m_SearchFirmId.ToString(), () => m_StoredNotes)
                        .EnableContainerLinqQuery<Note, Guid>(FakeCosmos.PersonNotesContainerName, m_SearchFirmId.ToString(), () => m_StoredNotes)
                        .EnableContainerFetch(FakeCosmos.PersonsContainerName, m_StoredPerson.Id.ToString(), m_SearchFirmId.ToString(), () => m_StoredPerson)
                        .EnableContainerFetchThrowCosmosException<Person>(FakeCosmos.PersonsContainerName, m_MissingPersonId.ToString(), m_SearchFirmId.ToString(), HttpStatusCode.NotFound)
                        .EnableContainerLinqQuery(FakeCosmos.SearchFirmsContainerName, m_SearchFirmId.ToString(), () => m_StoredUsers)
                        .EnableContainerLinqQuery(FakeCosmos.AssignmentsContainerName, m_SearchFirmId.ToString(), () => m_StoredAssignments);
        }

        [Theory]
        [InlineData(null, 3, false)]
        [InlineData(1, 1, true)]
        [InlineData(2, 2, true)]
        [InlineData(3, 3, false)]
        public async Task GetListReturnsCorrectResults(int? limit, int expectedResultNumber, bool expectedHasMoreResults)
        {
            // Given
            var controller = CreateController();
            Assert.True(m_StoredNotes[3].CreatedDate > m_StoredNotes[2].CreatedDate);
            Assert.True(m_StoredNotes[2].CreatedDate > m_StoredNotes[0].CreatedDate);
            m_Query.Limit = limit;

            // When
            var actionResult = await controller.GetList(m_StoredPerson.Id, m_Query);

            // Then
            var result = (GetList.Result)((OkObjectResult)actionResult).Value;
            Assert.NotEmpty(result.Notes);
            Assert.Equal(expectedResultNumber, result.Notes.Count);
            Assert.Equal(expectedHasMoreResults, result.HasMoreResults);
            Assert.Equal(3, result.Count);

            // Function to assert result items
            // ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local
            static void AssertExpectedResult(GetList.Result.Note resultNote, Note expectedStoredNoteMatch)
            {
                Assert.Equal(expectedStoredNoteMatch.Id, resultNote.Id);
                Assert.Equal(expectedStoredNoteMatch.PersonId, resultNote.PersonId);
                Assert.Equal(expectedStoredNoteMatch.NoteTitle, resultNote.NoteTitle);
                Assert.Equal(expectedStoredNoteMatch.NoteDescription, resultNote.NoteDescription);
                Assert.Equal(expectedStoredNoteMatch.AssignmentId, resultNote.AssignmentId);
                Assert.Equal(expectedStoredNoteMatch.CreatedDate.Date, resultNote.CreatedDate.Date);
                Assert.Equal(expectedStoredNoteMatch.CreatedBy, resultNote.CreatedByUserId);
                Assert.Equal(expectedStoredNoteMatch.UpdatedBy, resultNote.UpdatedByUserId);
                Assert.Null(resultNote.LinkCreatedByUser);
                Assert.Null(resultNote.LinkAssignment);
                Assert.Null(resultNote.LinkUpdatedByUser);
                Assert.Equal(expectedStoredNoteMatch.UpdatedDate, resultNote.UpdatedDate);
            }
            // ReSharper restore ParameterOnlyUsedForPreconditionCheck.Local

            if (expectedResultNumber >= 1)
                AssertExpectedResult(result.Notes[0], m_StoredNotes[3]);
            if (expectedResultNumber >= 2)
                AssertExpectedResult(result.Notes[1], m_StoredNotes[2]);
            if (expectedResultNumber >= 3) 
                AssertExpectedResult(result.Notes[2], m_StoredNotes[0]);
        }

        [Theory]
        [InlineData(new[] { GetList.Query.ExpandValue.CreatedByUser })]
        [InlineData(new[] { GetList.Query.ExpandValue.UpdatedByUser })]
        [InlineData(new[] { GetList.Query.ExpandValue.Assignment })]
        [InlineData(new[] { GetList.Query.ExpandValue.CreatedByUser, GetList.Query.ExpandValue.UpdatedByUser, GetList.Query.ExpandValue.Assignment })]
        public async Task GetListReturnsCorrectExpandedResults(GetList.Query.ExpandValue[] expand)
        {
            // Given
            var controller = CreateController();
            m_Query.Expand = new ExpandList<GetList.Query.ExpandValue>(expand);

            // When
            var actionResult = await controller.GetList(m_StoredPerson.Id, m_Query);

            // Then
            var result = (GetList.Result)((OkObjectResult)actionResult).Value;
            Assert.NotEmpty(result.Notes);
            Assert.Equal(3, result.Notes.Count);

            var firstNote = result.Notes[0];
            var secondNote = result.Notes[1];
            var thirdNote = result.Notes[2];
            
            if (expand.Contains(GetList.Query.ExpandValue.CreatedByUser))
            {
                Assert.NotNull(firstNote.LinkCreatedByUser);
                Assert.Equal(m_StoredUsers[0].Id, firstNote.LinkCreatedByUser.Id);
                Assert.Equal(m_StoredUsers[0].FirstName, firstNote.LinkCreatedByUser.FirstName);
                Assert.Equal(m_StoredUsers[0].LastName, firstNote.LinkCreatedByUser.LastName);

                Assert.NotNull(secondNote.LinkCreatedByUser);
                Assert.Equal(m_StoredUsers[0].Id, secondNote.LinkCreatedByUser.Id);
                Assert.Equal(m_StoredUsers[0].FirstName, secondNote.LinkCreatedByUser.FirstName);
                Assert.Equal(m_StoredUsers[0].LastName, secondNote.LinkCreatedByUser.LastName);

                Assert.NotNull(thirdNote.LinkCreatedByUser);
                Assert.Equal(m_StoredUsers[0].Id, thirdNote.LinkCreatedByUser.Id);
                Assert.Equal(m_StoredUsers[0].FirstName, thirdNote.LinkCreatedByUser.FirstName);
                Assert.Equal(m_StoredUsers[0].LastName, thirdNote.LinkCreatedByUser.LastName);

            }

            if (expand.Contains(GetList.Query.ExpandValue.UpdatedByUser))
            {
                Assert.Null(secondNote.LinkUpdatedByUser);
                Assert.Null(thirdNote.LinkUpdatedByUser);

                Assert.NotNull(firstNote.LinkUpdatedByUser);
                Assert.Equal(m_StoredUsers[1].Id, firstNote.LinkUpdatedByUser.Id);
                Assert.Equal(m_StoredUsers[1].FirstName, firstNote.LinkUpdatedByUser.FirstName);
                Assert.Equal(m_StoredUsers[1].LastName, firstNote.LinkUpdatedByUser.LastName);
            }

            if (expand.Contains(GetList.Query.ExpandValue.Assignment))
            {
                Assert.NotNull(secondNote.LinkAssignment);
                Assert.Equal(m_StoredAssignments[0].Id, secondNote.LinkAssignment.Id);
                Assert.Equal(m_StoredAssignments[0].Name, secondNote.LinkAssignment.Name);

                Assert.NotNull(firstNote.LinkAssignment);
                Assert.Equal(m_StoredAssignments[0].Id, firstNote.LinkAssignment.Id);
                Assert.Equal(m_StoredAssignments[0].Name, firstNote.LinkAssignment.Name);

                Assert.Null(thirdNote.LinkAssignment);
            }
        }

        [Fact]
        public async Task GetListReturnsCorrectAssignmentNotes()
        {
            // Given
            var controller = CreateController();
            m_Query.AssignmentId = m_StoredAssignments[0].Id;

            // When
            var actionResult = await controller.GetList(m_StoredPerson.Id, m_Query);

            // Then
            var result = (GetList.Result)((OkObjectResult)actionResult).Value;
            Assert.NotEmpty(result.Notes);
            Assert.Equal(2, result.Notes.Count);

            var resultNote = result.Notes[0];
            Assert.Equal(m_StoredNotes[3].Id, resultNote.Id);
            Assert.Equal(m_StoredNotes[3].NoteTitle, resultNote.NoteTitle);
            Assert.Equal(m_StoredNotes[3].NoteDescription, resultNote.NoteDescription);

            resultNote = result.Notes[1];
            Assert.Equal(m_StoredNotes[2].Id, resultNote.Id);
            Assert.Equal(m_StoredNotes[2].NoteTitle, resultNote.NoteTitle);
            Assert.Equal(m_StoredNotes[2].NoteDescription, resultNote.NoteDescription);
        }

        [Fact]
        public async Task GetListThrowsExceptionIfPersonDoesNotExist()
        {
            // Given
            var controller = CreateController();

            // When
            var ex = await Record.ExceptionAsync(() => controller.GetList(m_MissingPersonId, m_Query));

            // Then
            ex.AssertNotFoundFailure($"Unable to find 'Person' with Id '{m_MissingPersonId}'");
        }

        private NotesController CreateController()
        {
            return new ControllerBuilder<NotesController>()
                  .SetFakeCosmos(m_FakeCosmos)
                  .SetSearchFirmUser(m_SearchFirmId)
                  .SetFakeRepository(new FakeRepository())
                  .Build();
        }
    }
}
