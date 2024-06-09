using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Ikiru.Parsnips.Api.Controllers.Persons.Notes;
using Ikiru.Parsnips.UnitTests.Helpers;
using Ikiru.Parsnips.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Moq;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Persons.Notes
{
    public class PutTests
    {
        private readonly FakeCosmos m_FakeCosmos;
        private readonly Guid m_SearchFirmId = Guid.NewGuid();
        private readonly List<SearchFirmUser> m_SearchFirmUsers = new List<SearchFirmUser>();
        private readonly SearchFirmUser m_StoredUser2;
        private readonly Person m_Person;
        private readonly Person m_Person2;
        private readonly Note m_Note;
        private readonly Guid m_MissingNoteId = Guid.NewGuid();
        private readonly Put.Command m_Command;
        private readonly List<Assignment> m_StoredAssignments;

        public PutTests()
        {
            var storedUser1 = new SearchFirmUser(m_SearchFirmId)
                              {
                                  FirstName = "Current User FirstName",
                                  LastName = "Current User LastName"
                              };

            m_StoredUser2 = new SearchFirmUser(m_SearchFirmId)
                            {
                                FirstName = "Another User FirstName",
                                LastName = "Another User LastName"
                            };

            m_SearchFirmUsers.Add(storedUser1);
            m_SearchFirmUsers.Add(m_StoredUser2);

            m_StoredAssignments = new List<Assignment>
                                  {
                                      new Assignment(m_SearchFirmId) {Name = "Job"},
                                      new Assignment(m_SearchFirmId) {Name = "Job2"}
                                  };

            m_Person2 = new Person(m_SearchFirmId)
                        {
                            Name = "Person Two"
                        };

            m_Person = new Person(m_SearchFirmId)
                       {
                           Name = "Person One"
                       };

            m_Note = new Note(m_Person.Id, storedUser1.Id, m_SearchFirmId)
                     {
                         AssignmentId = m_StoredAssignments[0].Id,
                         NoteDescription = "This is the note description",
                         NoteTitle = "Note title"
                     };

            m_Command = new Put.Command
                        {
                            AssignmentId = m_StoredAssignments[1].Id,
                            NoteTitle = "Updated Title",
                            NoteDescription = "Updated description"
                        };

            m_FakeCosmos = new FakeCosmos()
                          .EnableContainerFetch(FakeCosmos.PersonNotesContainerName, m_Note.Id.ToString(), m_SearchFirmId.ToString(), () => m_Note)
                          .EnableContainerFetch(FakeCosmos.PersonsContainerName, m_Person.Id.ToString(), m_SearchFirmId.ToString(), () => m_Person)
                          .EnableContainerFetch(FakeCosmos.PersonsContainerName, m_Person.Id.ToString(), m_SearchFirmId.ToString(), () => m_Person2)
                          .EnableContainerReplace<Note>(FakeCosmos.PersonNotesContainerName, m_Note.Id.ToString(), m_SearchFirmId.ToString())
                          .EnableContainerLinqQuery(FakeCosmos.SearchFirmsContainerName, m_SearchFirmId.ToString(), () => m_SearchFirmUsers)
                          .EnableContainerFetchThrowCosmosException<Note>(FakeCosmos.PersonNotesContainerName, m_MissingNoteId.ToString(),
                                                                          m_SearchFirmId.ToString(), HttpStatusCode.NotFound)
                          .EnableContainerLinqQuery(FakeCosmos.AssignmentsContainerName, m_SearchFirmId.ToString(), () => m_StoredAssignments);
        }

        [Fact]
        public async Task PutUpdatesNoteInContainer()
        {
            // Given
            var controller = CreateController();

            // When
            var actionResult = await controller.Put(m_Person.Id, m_Note.Id, m_Command);

            // Then
            Assert.IsType<OkObjectResult>(actionResult);

            m_FakeCosmos.NotesContainer.Verify(nc => nc.ReplaceItemAsync(It.Is<Note>(note => note.Id == m_Note.Id &&
                                                                            note.NoteTitle == m_Command.NoteTitle &&
                                                                            note.NoteDescription == m_Command.NoteDescription &&
                                                                            note.UpdatedBy == m_StoredUser2.Id &&
                                                                            note.UpdatedDate.Value.Date
                                                                                == DateTimeOffset.Now.Date &&
                                                                            note.AssignmentId == m_Command.AssignmentId),
                                                                            m_Note.Id.ToString(),
                                                                          It.Is<PartitionKey>(p => p == new PartitionKey(m_SearchFirmId.ToString())),
                                                                          It.IsAny<ItemRequestOptions>(),
                                                                          It.IsAny<CancellationToken>()));


        }

        [Fact]
        public async Task PutExistingNoteDelinkAssignmentReturnsUpdatedNote()
        {
            // Given
            var controller = CreateController();
            m_Command.AssignmentId = null;

            // When
            var actionResult = await controller.Put(m_Person.Id, m_Note.Id, m_Command);

            // Then
            var result = (Put.Result)((OkObjectResult)actionResult).Value;

            Assert.Equal(m_Command.NoteDescription, result.NoteDescription);
            Assert.Equal(m_Command.NoteTitle, result.NoteTitle);
            Assert.Equal(m_Command.Id, result.Id);
            Assert.Equal(m_Person.Id, result.PersonId);
            Assert.Null(result.LinkAssignment);
            Assert.Null(result.AssignmentId);
            Assert.Equal(DateTimeOffset.Now.Date, result.UpdatedDate.Date);
        }

        [Fact]
        public async Task PutExistingNoteRetunsUpdatedNote()
        {
            // Given
            var controller = CreateController();

            // When
            var actionResult = await controller.Put(m_Person.Id, m_Note.Id, m_Command);

            // Then
            var result = (Put.Result)((OkObjectResult)actionResult).Value;

            Assert.Equal(m_Command.NoteDescription, result.NoteDescription);
            Assert.Equal(m_Command.NoteTitle, result.NoteTitle);
            Assert.Equal(m_Command.Id, result.Id);
            Assert.Equal(m_Person.Id, result.PersonId);
            Assert.Equal(m_StoredAssignments[1].Id, result.LinkAssignment.Id);
            Assert.Equal(m_StoredAssignments[1].Name, result.LinkAssignment.Name);
            Assert.Equal(m_StoredUser2.Id, result.LinkUpdatedByUser.Id);
            Assert.Equal(DateTimeOffset.Now.Date, result.UpdatedDate.Date);
        }


        [Fact]
        public async Task PutThrowsWhenNoteNotExist()
        {
            // Given
            var controller = CreateController();
       
            // When
            var ex = await Record.ExceptionAsync(() => controller.Put(m_Person.Id, m_MissingNoteId, m_Command));

            // Then 
            ex.AssertNotFoundFailure($"Unable to find '{nameof(Note)}' with Id '{m_Command.Id}'");

        }

        [Fact]
        public async Task PutThrowsWhenPersonNotAssociatedWithNote()
        {
            // Given
            var controller = CreateController();
         
            // When
            var ex = await Record.ExceptionAsync(() => controller.Put(m_Person2.Id, m_Note.Id, m_Command));

            // Then 
            ex.AssertNotFoundFailure($"Unable to find '{nameof(Note)}' with Id '{m_Note.Id}'");
        }


        private NotesController CreateController()
        {
            return new ControllerBuilder<NotesController>()
                  .SetFakeCosmos(m_FakeCosmos)
                  .SetSearchFirmUser(m_SearchFirmId, m_StoredUser2.Id)
                  .Build();
        }
    }
}
