using AutoMapper;
using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Application.Services;
using Ikiru.Parsnips.Application.Services.Assignment;
using Ikiru.Parsnips.Application.Services.Notes.Models;
using Ikiru.Parsnips.Application.Shared.Mappings;
using Ikiru.Parsnips.Domain.Notes;
using Ikiru.Persistence.Repository;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Application.Services.Assignment
{
    public class AssignmentServicesTests
    {
        private readonly Mock<IRepository> _repositoryMock;
        private readonly Mock<INoteService> _noteService;
        private readonly IMapper _mapper;

        public AssignmentServicesTests()
        {
            _repositoryMock = new Mock<IRepository>();
            _noteService = new Mock<INoteService>();
            var config = new MapperConfiguration(cfg => cfg.AddProfile(typeof(ApplicationMappingProfile)));
            config.AssertConfigurationIsValid();
            _mapper = config.CreateMapper();
        }

        [Fact]
        public async Task CreateAssignmentNoteWithValidData()
        {
            Guid personId = Guid.Empty;
            Guid createdBy = Guid.NewGuid();
            Guid searchFirmId = Guid.NewGuid();
            string title = "note title";
            string description = "note description";
            DateTimeOffset createdDate = DateTimeOffset.Now;

            var assignment = new Domain.Assignment(searchFirmId)
            {
            };

            _repositoryMock
                .Setup(r => r.GetItem<Domain.Assignment>(It.Is<string>(pk => pk == searchFirmId.ToString()), It.Is<string>(id => id == assignment.Id.ToString())))
                .ReturnsAsync(assignment);

            _noteService.Setup(n => n.CreateNote(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
                                                It.IsAny<DateTimeOffset>()))
                .ReturnsAsync(new ServiceResponse<NoteResponse>()
                {
                    Value = new NoteResponse()
                    {
                        Note = new Domain.Note(personId, createdBy, searchFirmId)
                        {
                            AssignmentId = assignment.Id,
                            NoteTitle = title,
                            NoteDescription = description
                        }
                    }
                });

            var assignmentService = new AssignmentService(new AssignmentRepository(_repositoryMock.Object), null,
                                                            _noteService.Object,
                                                            _mapper, null, null, null, null, null, null);

            var response = await assignmentService.CreateAssignmentNote(assignment.Id, createdBy, searchFirmId, title, description, createdDate);

            Assert.False(response.ValidationErrors.Any());
            Assert.True(response.Value.Note.NoteTitle == title);
            Assert.True(response.Value.Note.NoteDescription == description);
            Assert.True(response.Value.Note.CreatedBy == createdBy);
            Assert.True(response.Value.Note.CreatedDate.Date == createdDate.Date);
            Assert.False(response.Value.Note.Id == Guid.Empty);

            _repositoryMock.Verify(x => x.UpdateItem(It.Is<Domain.Assignment>(a => a.Id == assignment.Id &&
                                                                            a.Notes.Contains(response.Value.Note.Id))), Times.Once);
        }

        [Fact]
        public async Task CreateAssignmentNoteWithInValidDataReturnError()
        {
            Guid assignmentId = Guid.NewGuid();
            Guid personId = Guid.NewGuid();
            Guid createdBy = Guid.NewGuid();
            Guid searchFirmId = Guid.NewGuid();
            string title = "note title";
            string description = "note description";
            DateTimeOffset createdDate = DateTimeOffset.Now;


            _noteService.Setup(n => n.CreateNote(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
                                                It.IsAny<DateTimeOffset>()))
                .ReturnsAsync(new ServiceResponse<NoteResponse>()
                {
                    Value = null,
                    ValidationErrors = new List<System.ComponentModel.DataAnnotations.ValidationResult>
                    {
                        new System.ComponentModel.DataAnnotations.ValidationResult("blah error")
                    }
                });

            var assignmentService = new AssignmentService(new AssignmentRepository(_repositoryMock.Object), null,
                                                            _noteService.Object,
                                                            _mapper, null, null, null, null, null, null);

            var response = await assignmentService.CreateAssignmentNote(assignmentId, createdBy, searchFirmId, title, description, createdDate);

            Assert.True(response.ValidationErrors.Any());
            Assert.Null(response.Value);

            _repositoryMock.Verify(x => x.Add(It.Is<Domain.Assignment>(a => a.Id == assignmentId &&
                                                                            a.Notes.Contains(response.Value.Note.Id))), Times.Never);

        }



        #region refactored code tests

        [Fact]
        public async Task CreateAssignmentNote()
        {
            // Arrange

            Mock<IRepository> repository = new Mock<IRepository>();

            var assignmentService = new AssignmentService(null, null, null, null, null, null, null, null,
                                                        new NoteRepository(repository.Object), null);

            // Act
            await assignmentService.CreateNote(default, default, default, default, default, default, default);

            // Assert
            repository.Verify(r => r.Add(It.IsAny<AssignmentNote>()));

        }


        [Fact]
        public async Task CreateAssignmentNoteExceptionNoteNotFound()
        {
            // Arrange

            Mock<IRepository> repository = new Mock<IRepository>();

            var assignmentService = new AssignmentService(null, null, null, null, null, null, null, null,
                                                        new NoteRepository(repository.Object), null);

            // Act
            await assignmentService.CreateNote(default, default, default, default, default, default, default);

            // Assert
            repository.Verify(r => r.Add(It.IsAny<AssignmentNote>()));

        }

        [Fact]
        public async Task UpdateAssignmentNote()
        {
            // Arrange

            Mock<IRepository> repository = new Mock<IRepository>();

            var searchFirmId = Guid.Empty;

            var assignmentNote = new AssignmentNote(default, default, default, "title", "text", default, default, default);

            repository.Setup(r => r.GetByQuery(It.IsAny<Expression<Func<AssignmentNote, bool>>>()))
                    .Returns(Task.FromResult(new List<AssignmentNote>()
                    {
                        assignmentNote
                    }));

            var assignmentService = new AssignmentService(null, null, null, null, null, null, null, null,
                                                        new NoteRepository(repository.Object), null);

            // Act
            await assignmentService.UpdateNote(default, "title1", "text1", default, default, default, default, default);

            // Assert

            repository.Verify(r => r.GetByQuery(It.IsAny<Expression<Func<AssignmentNote, bool>>>()));
            repository.Verify(r => r.UpdateItem(It.IsAny<AssignmentNote>()));

        }


        [Fact]
        public async Task UpdateAssignmentNoteInvalidDetailsThrowException()
        {
            // Arrange

            Mock<IRepository> repository = new Mock<IRepository>();

            var searchFirmId = Guid.Empty;

            var assignmentNote = new AssignmentNote(default, default, default, "title", "text", default, default, default);

            repository.Setup(r => r.GetByQuery(It.IsAny<Expression<Func<AssignmentNote, bool>>>()))
                    .Returns(Task.FromResult(new List<AssignmentNote>()
                    {
                        assignmentNote
                    }));

            var assignmentService = new AssignmentService(null, null, null, null, null, null, null, null,
                                                        new NoteRepository(repository.Object), null);

            // Assert
            await Assert.ThrowsAsync<Exception>(() => assignmentService.UpdateNote(default, new String('a', 101), "text1", default, default, default, default, default));
            repository.Verify(r => r.GetByQuery(It.IsAny<Expression<Func<AssignmentNote, bool>>>()), Times.Once);
            repository.Verify(r => r.UpdateItem(It.IsAny<AssignmentNote>()), Times.Never);

        }

        [Fact]
        public async Task UpdateAssignmentNoteNotFound()
        {
            // Arrange
            Mock<IRepository> repository = new Mock<IRepository>();
            var assignmentService = new AssignmentService(null, null, null, null, null, null, null, null,
                                                       new NoteRepository(repository.Object), null);

            // Assert
            await Assert.ThrowsAsync<Exception>(() => assignmentService.UpdateNote(default, "title1", "text1", default, default, default, default, default));

        }

        #endregion 

    }
}
