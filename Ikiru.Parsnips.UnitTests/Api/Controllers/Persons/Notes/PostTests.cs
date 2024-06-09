using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ikiru.Parsnips.Api.Controllers.Persons.Notes;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.UnitTests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using System.Net;
using System.Threading;
using Microsoft.Azure.Cosmos;
using Moq;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Persons.Notes
{
    public class PostTests
    {
        private readonly Guid m_SearchFirmId = Guid.NewGuid();
        
        private readonly SearchFirmUser m_StoredUser;

        private readonly Person m_Person;
        private readonly Guid m_MissingPersonId = Guid.NewGuid();

        private readonly Assignment m_StoredAssignment;
        private readonly Guid m_MissingAssignmentId = Guid.NewGuid();
        
        private readonly Post.Command m_Command = new Post.Command
                                                  {
                                                      NoteTitle = "Test title",
                                                      NoteDescription = "Test Description"
                                                  };

        private readonly FakeCosmos m_FakeCosmos;

        public PostTests()
        {
            m_StoredUser = new SearchFirmUser(m_SearchFirmId)
                           {
                               FirstName = "Current User FirstName",
                               LastName = "Current User LastName"
                           };

            m_StoredAssignment = new Assignment(m_SearchFirmId)
                                 {
                                     Name = "Job"
                                 };

            m_Person = new Person(m_SearchFirmId)
            {
                Name = "Notes Person"
            };

            m_FakeCosmos = new FakeCosmos()
                          .EnableContainerFetch(FakeCosmos.PersonsContainerName, m_Person.Id.ToString(), m_SearchFirmId.ToString(), () => m_Person)
                          .EnableContainerFetchThrowCosmosException<Person>(FakeCosmos.PersonsContainerName, m_MissingPersonId.ToString(), m_SearchFirmId.ToString(), HttpStatusCode.NotFound)
                          .EnableContainerInsert<Note>(FakeCosmos.PersonNotesContainerName)
                          .EnableContainerLinqQuery(FakeCosmos.SearchFirmsContainerName, m_SearchFirmId.ToString(), () => new List<SearchFirmUser> { m_StoredUser })
                          .EnableContainerLinqQuery(FakeCosmos.AssignmentsContainerName, m_SearchFirmId.ToString(), () => new List<Assignment> { m_StoredAssignment });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task PostReturnsCreatedNote(bool specifyAssignment)
        {
            // Given
            if (specifyAssignment)
                m_Command.AssignmentId = m_StoredAssignment.Id;

            var controller = CreateController();

            // When
            var actionResult = await controller.Post(m_Person.Id, m_Command);

            // Then
            var result = (Post.Result)((OkObjectResult)actionResult).Value;
            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Equal(m_Person.Id, result.PersonId);
            Assert.Equal(m_Command.NoteTitle, result.NoteTitle);
            Assert.Equal(m_Command.NoteDescription, result.NoteDescription);
            Assert.Equal(DateTimeOffset.UtcNow.Date, result.CreatedDate.Date);
            Assert.Equal(m_StoredUser.Id, result.CreatedByUserId);
            Assert.NotNull(result.LinkCreatedByUser);
            Assert.Equal(m_StoredUser.Id, result.LinkCreatedByUser.Id);
            Assert.Equal(m_StoredUser.FirstName, result.LinkCreatedByUser.FirstName);
            Assert.Equal(m_StoredUser.LastName, result.LinkCreatedByUser.LastName);

            Assert.Equal(m_Command.AssignmentId, result.AssignmentId);

            if (!specifyAssignment)
            {
                Assert.Null(result.LinkAssignment);
            }
            else
            {
                Assert.NotNull(result.LinkAssignment);
                Assert.Equal(m_StoredAssignment.Id, result.LinkAssignment.Id);
                Assert.Equal(m_StoredAssignment.Name, result.LinkAssignment.Name);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task PostCreatesNoteInDb(bool specifyAssignment)
        {
            // Given
            if (specifyAssignment)
                m_Command.AssignmentId = m_StoredAssignment.Id;

            var controller = CreateController();

            // When
            await controller.Post(m_Person.Id, m_Command);

            // Then
            m_FakeCosmos.NotesContainer.Verify(x => x.CreateItemAsync(It.Is<Note>(n => n.Id != Guid.Empty &&
                                                                                       n.CreatedDate.Date == DateTimeOffset.UtcNow.Date &&
                                                                                       n.CreatedBy == m_StoredUser.Id &&
                                                                                       n.NoteTitle == m_Command.NoteTitle &&
                                                                                       n.NoteDescription == m_Command.NoteDescription &&
                                                                                       n.PersonId == m_Person.Id &&
                                                                                       n.AssignmentId == m_Command.AssignmentId &&
                                                                                       n.UpdatedDate == null &&
                                                                                       n.UpdatedBy == null),
                                                                      It.Is<PartitionKey>(p => p == new PartitionKey(m_SearchFirmId.ToString())),
                                                                      It.IsAny<ItemRequestOptions>(),
                                                                      It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task PostThrowsErrorWhenPersonInvalid()
        {
            // Given
            var controller = CreateController();

            // When
            var ex = await Record.ExceptionAsync(() => controller.Post(m_MissingPersonId, m_Command));

            // Then
            ex.AssertNotFoundFailure(nameof(Person), true);
        }

        [Fact]
        public async Task PostThrowsErrorIfAssignmentIdNotFound()
        {
            // Given
            m_Command.AssignmentId = m_MissingAssignmentId;
            var controller = CreateController();

            // When
            var ex = await Record.ExceptionAsync(() => controller.Post(m_Person.Id, m_Command));

            // Then
            ex.AssertParamValidationFailure(nameof(Post.Command.AssignmentId), "The provided {Param} does not exist.");
        }

        private NotesController CreateController()
        {
            return new ControllerBuilder<NotesController>()
                  .SetFakeCosmos(m_FakeCosmos)
                  .SetSearchFirmUser(m_SearchFirmId, m_StoredUser.Id)
                  .SetFakeRepository(new FakeRepository())
                  .Build();
        }
    }
}
