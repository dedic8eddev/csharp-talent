using Ikiru.Parsnips.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.DomainModels
{
    public class NoteTests
    {
        [Fact]
        public void NotesIsValid()
        {
           
            var title = new string('a', 10);
            var description = new string('a', 50);
            var createdByUserId = Guid.NewGuid();
            var note = new Note(default, createdByUserId, default);

            note.NoteDescription = description;
            note.NoteTitle = title;
            note.AssignmentId = Guid.NewGuid();
            note.Validate();

            Assert.Equal(DateTimeOffset.Now.Date, note.CreatedDate.Date);
            Assert.NotEqual(Guid.Empty, note.Id);
            Assert.False(note.ValidationResults.Any());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("GenerateMaxLength")]
        public void NotesTitleIsInValid(string noteTitle = null)
        {
            var title = noteTitle == "GenerateMaxLength"
                        ? new String('a', 101)
                        : noteTitle;

            var note = new Note(default, default, default);
            var description = new string('a', 50);

            note.NoteDescription = description;
            note.NoteTitle = title;
            note.AssignmentId = Guid.NewGuid();
            note.Validate();

            Assert.True(note.ValidationResults.Exists(a => a.MemberNames.Contains(nameof(Note.NoteTitle))));
        }

        [Fact]
        public void NotesAssignmentIdNotSet()
        {
            var note = new Note(default, default, default);
            var description = new string('a', 50);

            note.NoteDescription = description;
            note.NoteTitle = "asdf";
            note.Validate();

            Assert.True(note.ValidationResults.Exists(a => a.MemberNames.Contains(nameof(Note.AssignmentId))));
        }

        [Fact]
        public void NotesUpdateIsValid()
        {

            var title = new string('a', 10);
            var description = new string('a', 50);
            var createdByUserId = Guid.NewGuid();
            var updatedByUserId = Guid.NewGuid();
            var udpatedDate = DateTimeOffset.Now.AddMinutes(1);
            var assignmentId = Guid.NewGuid(); 
            var note = new Note(default, createdByUserId, default);

            note.NoteDescription = description;
            note.NoteTitle = title;
            note.AssignmentId = assignmentId;

            note.UpdatedBy = updatedByUserId;
            note.UpdatedDate = udpatedDate;

            note.Validate();

            Assert.Equal(DateTimeOffset.Now.Date, note.UpdatedDate.Value.Date);
            Assert.NotEqual(Guid.Empty, note.Id);
            Assert.False(note.ValidationResults.Any());
        }

        [Fact]
        public void NotesUpdateIsInValid()
        {

            var title = new string('a', 10);
            var description = new string('a', 50);
            var createdByUserId = Guid.NewGuid();
            var updatedByUserId = Guid.NewGuid();
            var udpatedDate = DateTimeOffset.Now.AddMinutes(1);
            var assignmentId = Guid.NewGuid();
            var note = new Note(default, createdByUserId, default);

            note.NoteDescription = description;
            note.NoteTitle = title;
            note.AssignmentId = assignmentId;

            note.UpdatedBy = updatedByUserId;

            note.Validate();

            Assert.NotEqual(Guid.Empty, note.Id);
            Assert.True(note.ValidationResults.Any());
        }


    }
}
