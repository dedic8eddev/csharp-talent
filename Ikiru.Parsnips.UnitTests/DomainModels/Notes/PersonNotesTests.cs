using Ikiru.Parsnips.Domain.Notes;
using System;
using System.Linq;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.DomainModels.Notes
{
    public class PersonNotesTests
    {
        [Fact]
        public void CreateAPersonNote()
        {
            // Arrange
            var personId = Guid.NewGuid();
            var searchFirmId = Guid.NewGuid();
            var createdByUserId = Guid.NewGuid();
            var assignmentId = Guid.NewGuid();
            var title = $"note title 1 - {Guid.NewGuid()}";
            var text = $"note text 1 - {Guid.NewGuid()}";
            NoteTypeEnum type = NoteTypeEnum.CandidateProgress;
            ContactMethodEnum contactMethod = ContactMethodEnum.Email;


            // Act
            var personNote = new PersonNote(searchFirmId: searchFirmId,
                                            createdByUserId: createdByUserId,
                                            assignmentId: assignmentId,
                                            personId: personId,
                                            title: title,
                                            text: text,
                                            type: type,
                                            contactMethod: contactMethod);

            var validationResults = personNote.Validate();

            // Assert
            Assert.False(validationResults.Any());
            Assert.Equal(personId, personNote.PersonId);
            Assert.Equal(searchFirmId, searchFirmId);
            Assert.Equal(createdByUserId, createdByUserId);
            Assert.Equal(text, personNote.Text);
            Assert.Equal(type, personNote.Type);
            Assert.Equal(assignmentId, personNote.AssignmentId);
            Assert.Equal(contactMethod, personNote.ContactMethod);
            Assert.Same(personNote.Descriminator, nameof(PersonNote));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("GenerateALongString")]
        public void CreateAPersonNoteFailValidation(string title)
        {
            // Arrange
            var personId = Guid.NewGuid();
            var searchFirmId = Guid.NewGuid();
            var createdByUserId = Guid.NewGuid();
            title = title != null ? new string('a', 101) : title;
            
            var text = $"note text 1 - {Guid.NewGuid()}";
            NoteTypeEnum type = NoteTypeEnum.CandidateProgress;
            ContactMethodEnum contactMethod = ContactMethodEnum.Email;


            // Act
            var personNote = new PersonNote(searchFirmId: searchFirmId,
                                            createdByUserId: createdByUserId,
                                            assignmentId: null,
                                            personId: personId,
                                            title: title,
                                            text: text,
                                            type: type,
                                            contactMethod: contactMethod);

            var validationResults = personNote.Validate();

            // Assert
            Assert.True(validationResults.Any());
            Assert.Equal(searchFirmId, searchFirmId);
            Assert.Equal(createdByUserId, createdByUserId);
            Assert.Equal(text, personNote.Text);
            Assert.Equal(type, personNote.Type);
            Assert.Equal(contactMethod, personNote.ContactMethod);
        }

        [Fact]
        public void UpdatePersonNote()
        {
            // Arrange
            var personId = Guid.NewGuid();
            var searchFirmId = Guid.NewGuid();
            var createdByUserId = Guid.NewGuid();
            var assignmentId = Guid.NewGuid();
            var title = $"note title 1 - {Guid.NewGuid()}";
            var text = $"note text 1 - {Guid.NewGuid()}";
            NoteTypeEnum type = NoteTypeEnum.CandidateProgress;
            ContactMethodEnum contactMethod = ContactMethodEnum.Email;

            var updateTitle = $"update note title 1 - {Guid.NewGuid()}";
            var updateText = $"update note text 1 - {Guid.NewGuid()}";
            NoteTypeEnum updateType = NoteTypeEnum.Businessdevelopment;
            ContactMethodEnum updateContactMethod = ContactMethodEnum.Call;
            var lastEdited = DateTimeOffset.Now;
            var lastEditedBy = Guid.NewGuid();

            // Act
            var personNote = new PersonNote(searchFirmId: searchFirmId,
                                            createdByUserId: createdByUserId,
                                            assignmentId: assignmentId,
                                            personId: personId,
                                            title: title,
                                            text: text,
                                            type: type,
                                            contactMethod: contactMethod);

            personNote.Update(title: updateTitle,
                                text: updateText,
                                type: updateType,
                                contactMethod: updateContactMethod,
                                lastEdited: lastEdited,
                                lastEditedBy: lastEditedBy);

            var validationResults = personNote.Validate();
            Assert.False(validationResults.Any());
            Assert.Equal(updateText, personNote.Text);
            Assert.Equal(updateType, personNote.Type);
            Assert.Equal(updateContactMethod, personNote.ContactMethod);
            Assert.Equal(lastEdited, personNote.LastEdited);
            Assert.Equal(lastEditedBy, personNote.LastEditedBy);

        }

    }
}
