using Ikiru.Parsnips.Domain.Notes;
using System;
using System.Linq;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.DomainModels.Notes
{
    public class AssignmentNotesTests
    {
        [Fact]
        public void CreateAssaignmentNote()
        {
            // Arrange
            var searchFirmId = Guid.NewGuid();
            var createdByUserId = Guid.NewGuid();
            var assignmentId = Guid.NewGuid();
            var title = $"note title 1 - {Guid.NewGuid()}";
            var text = $"note text 1 - {Guid.NewGuid()}";
            NoteTypeEnum type = NoteTypeEnum.CandidateProgress;
            ContactMethodEnum contactMethod = ContactMethodEnum.Email;


            // Act
            var assignmentNote = new AssignmentNote(searchFirmId: searchFirmId,
                                            createdByUserId: createdByUserId,
                                            assignmentId: assignmentId,
                                            title: title,
                                            text: text,
                                            type: type,
                                            contactMethod: contactMethod);

            var validationResults = assignmentNote.Validate();

            // Assert
            Assert.False(validationResults.Any());
            Assert.Equal(searchFirmId, searchFirmId);
            Assert.Equal(createdByUserId, createdByUserId);
            Assert.Equal(text, assignmentNote.Text);
            Assert.Equal(type, assignmentNote.Type);
            Assert.Equal(assignmentId, assignmentNote.AssignmentId);
            Assert.Equal(contactMethod, assignmentNote.ContactMethod);
            Assert.Same(assignmentNote.Descriminator, nameof(AssignmentNote));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("GenerateALongString")]
        public void CreateAssignmentNoteFailValidation(string title)
        {
            // Arrange
            var personId = Guid.NewGuid();
            var searchFirmId = Guid.NewGuid();
            var createdByUserId = Guid.NewGuid();
            var assignmentId = Guid.NewGuid();
            title = title != null ? new string('a', 101) : title;

            var text = $"note text 1 - {Guid.NewGuid()}";
            NoteTypeEnum type = NoteTypeEnum.CandidateProgress;
            ContactMethodEnum contactMethod = ContactMethodEnum.Email;


            // Act
            var assignmentNote = new AssignmentNote(searchFirmId: searchFirmId,
                                            createdByUserId: createdByUserId,
                                            assignmentId: assignmentId,
                                            title: title,
                                            text: text,
                                            type: type,
                                            contactMethod: contactMethod);

            var validationResults = assignmentNote.Validate();

            // Assert
            Assert.True(validationResults.Any());
            Assert.Equal(searchFirmId, searchFirmId);
            Assert.Equal(createdByUserId, createdByUserId);
            Assert.Equal(text, assignmentNote.Text);
            Assert.Equal(type, assignmentNote.Type);
            Assert.Equal(contactMethod, assignmentNote.ContactMethod);
        }

        [Fact]
        public void UpdateAssignment()
        {
            // Arrange
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
            var assignmentNote = new AssignmentNote(searchFirmId: searchFirmId,
                                            createdByUserId: createdByUserId,
                                            assignmentId: assignmentId,
                                            title: title,
                                            text: text,
                                            type: type,
                                            contactMethod: contactMethod);

            assignmentNote.Update(title: updateTitle,
                                    text: updateText,
                                    type: updateType,
                                    contactMethod: updateContactMethod,
                                    lastEdited: lastEdited,
                                    lastEditedBy: lastEditedBy);

            var validationResults = assignmentNote.Validate();
            Assert.False(validationResults.Any());
            Assert.Equal(updateText, assignmentNote.Text);
            Assert.Equal(updateType, assignmentNote.Type);
            Assert.Equal(updateContactMethod, assignmentNote.ContactMethod);
            Assert.Equal(lastEdited, assignmentNote.LastEdited);
            Assert.Equal(lastEditedBy, assignmentNote.LastEditedBy);

        }
    }
}

