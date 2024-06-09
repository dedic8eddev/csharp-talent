using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Notes;
using Ikiru.Persistence.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Application.Persistence
{
    public class NoteRepository : RepositoryBase<Ikiru.Parsnips.Domain.Note>
    {
        private IRepository _repository;

        public NoteRepository(IRepository repository) : base(repository)
        {
            _repository = repository;
        }

        public async Task<Ikiru.Parsnips.Domain.Note> GetLatestNoteForPerson(Guid personId, Guid searchFirmId)
        {
            var notes = await _repository.GetByQuery<Ikiru.Parsnips.Domain.Note>(n => n.PersonId == personId && n.SearchFirmId == searchFirmId);

            return notes.OrderByDescending(n => n.CreatedDate).FirstOrDefault();
        }

        public Task<List<Ikiru.Parsnips.Domain.Note>> GetNotesByIds(Guid searchFirmId, IEnumerable<Guid> noteIds)
            => _repository.GetByQuery<Ikiru.Parsnips.Domain.Note, Ikiru.Parsnips.Domain.Note>(searchFirmId.ToString(), i => i.Where(n => noteIds.Contains(n.Id)));


        #region Refectored notes

        public async Task CreateAssignmentNote(AssignmentNote assignmentNote)
        {
            await _repository.Add(assignmentNote);
        }

        public async Task<AssignmentNote> GetAssignmentNoteById(Guid assignmentNoteId)
        {
            var assignmentnote = await _repository.GetByQuery<AssignmentNote>(x => x.Id == assignmentNoteId);

            if (assignmentnote == null)
            {
                return default;
            }

            return assignmentnote.FirstOrDefault();

        }

        public async Task<AssignmentNote> UpdateAssignmentNote(AssignmentNote assignmentNote)
        {
            return await _repository.UpdateItem(assignmentNote);
        }


        public async Task CreatePersonNote(PersonNote personNote)
        {
            await _repository.Add(personNote);
        }

        public async Task<PersonNote> GetPersonNoteById(Guid personNoteId)
        {
            var personNotes = await _repository.GetByQuery<PersonNote>(x => x.Id == personNoteId);

            if (personNotes == null)
            {
                return default;
            }

            return personNotes.FirstOrDefault();

        }

        public async Task<PersonNote> UpdatePersonNote(PersonNote personNote)
        {
            return await _repository.UpdateItem(personNote);
        }


        #endregion
    }
}
