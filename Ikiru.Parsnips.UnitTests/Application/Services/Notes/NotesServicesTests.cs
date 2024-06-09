using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Application.Services;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.UnitTests.Helpers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Application.Services.Notes
{
    public class NotesServicesTests
    {
        [Fact]
        public async Task GetNotesInOrder()
        {
            // Arrange
            var fakeRepository = new FakeRepository();

            var searchFirm = new SearchFirm();
            await fakeRepository.Add(searchFirm);

            var searchFirmUser = new SearchFirmUser(searchFirm.Id);
            await fakeRepository.Add(searchFirmUser);

            var note1 = new Note(Guid.NewGuid(), searchFirmUser.Id, searchFirm.Id) { NoteTitle = "title 1" };
            Thread.Sleep(1000);
            var note2 = new Note(Guid.NewGuid(), searchFirmUser.Id, searchFirm.Id);
            Thread.Sleep(1000);
            var note3 = new Note(Guid.NewGuid(), searchFirmUser.Id, searchFirm.Id) { NoteTitle = "title 3" };
            Thread.Sleep(1000);
            note2.UpdatedBy = searchFirmUser.Id;
            note2.UpdatedDate = DateTimeOffset.Now;

            var noteIds = new List<Guid>() { note1.Id, note2.Id, note3.Id };

            await fakeRepository.Add(note1);
            await fakeRepository.Add(note2);
            await fakeRepository.Add(note3);


            var noteService = new NoteService(new NoteRepository(fakeRepository), new SearchFirmRepository(fakeRepository));

            // Act
            var notes = await noteService.GetNotes(searchFirmUser.SearchFirmId, noteIds);

            // Assert
            Assert.Equal(notes.Value[0].Note.NoteTitle, note2.NoteTitle);
            Assert.Equal(notes.Value[1].Note.NoteTitle, note3.NoteTitle);
            Assert.Equal(notes.Value[2].Note.NoteTitle, note1.NoteTitle);


        }
    }
}
