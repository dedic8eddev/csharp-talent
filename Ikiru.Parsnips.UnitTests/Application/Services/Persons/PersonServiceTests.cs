using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Application.Services.Person;
using Ikiru.Parsnips.Domain.Notes;
using Ikiru.Persistence.Repository;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Application.Services.Persons
{
    public class PersonServiceTests
    {
        [Fact]
        public async Task CreatePersonNote()
        {
            // Arrange
            Mock<IRepository> repository = new Mock<IRepository>();

            var personService = new PersonService(new NoteRepository(repository.Object),default,default,default,default,default,default);

            // Act
            await personService.CreateNote(default, default, default, default, "Title", "Text", default, default);

            // Assert
            repository.Verify(r => r.Add(It.IsAny<PersonNote>()), Times.Once);

        }

        [Fact]
        public async Task CreateInvalidPersonNoteException()
        {
            // Arrange
            Mock<IRepository> repository = new Mock<IRepository>();

            var personService = new PersonService(new NoteRepository(repository.Object), default, default, default, default, default, default);

            // Assert
            await Assert.ThrowsAsync<Exception>(() => personService.CreateNote(default, default, default, default, new string('a', 101), "text", default, default));
            repository.Verify(r => r.Add(It.IsAny<AssignmentNote>()), Times.Never);

        }

        [Fact]
        public async Task UpdatePersonNote()
        {
            // Arrange

            Mock<IRepository> repository = new Mock<IRepository>();

            var searchFirmId = Guid.Empty;

            var personNote = new PersonNote(default, default, default, "title", "text", default, default, default);

            repository.Setup(r => r.GetByQuery(It.IsAny<Expression<Func<PersonNote, bool>>>()))
                    .Returns(Task.FromResult(new List<PersonNote>()
                    {
                        personNote
                    }));

            var personService = new PersonService(new NoteRepository(repository.Object), default, default, default, default, default, default);

            // Act
            await personService.UpdateNote(default, default, default, "title1", "text1", default, default, default, default);

            // Assert
            repository.Verify(r => r.GetByQuery(It.IsAny<Expression<Func<PersonNote, bool>>>()));
            repository.Verify(r => r.UpdateItem(It.IsAny<PersonNote>()));

        }

        [Fact]
        public async Task UpdatePersonNoteNotFound()
        {
            // Arrange
            Mock<IRepository> repository = new Mock<IRepository>();

            var personService = new PersonService(new NoteRepository(repository.Object), default, default, default, default, default, default);

            // Assert
            await Assert.ThrowsAsync<Exception>(() => personService.UpdateNote(default, default, default, "title1", "text1", default, default, default, default));

        }
    }
}
