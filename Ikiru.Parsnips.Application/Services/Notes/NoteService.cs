using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Application.Services.Notes.Models;
using Ikiru.Parsnips.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Application.Services
{
    public class NoteService : INoteService
    {
        private readonly NoteRepository _noteRepository;
        private readonly SearchFirmRepository _searchFirmRepository;

        public NoteService(NoteRepository noteRepository, SearchFirmRepository searchFirmRepository)
        {
            _noteRepository = noteRepository;
            _searchFirmRepository = searchFirmRepository;
        }

        public async Task<ServiceResponse<NoteResponse>> CreateNote(Guid assignmentId, Guid personId, Guid createdBy, Guid searchFirmId,
                                              string title, string description, DateTimeOffset createdDate)
        {
            var servicesResponse = new ServiceResponse<NoteResponse>()
            {
                Value = new NoteResponse()
            };

            var note = new Note(personId, createdBy, searchFirmId)
            {
                NoteTitle = title,
                NoteDescription = description,
                AssignmentId = assignmentId
            };

            var validationResults = note.Validate();

            if (validationResults.Any())
            {
                servicesResponse.ValidationErrors = validationResults;
            }
            else
            {
                servicesResponse.Value.Note = await _noteRepository.Create(note);
                servicesResponse.Value.CreatedBy = await _searchFirmRepository.GetUserById(note.SearchFirmId, note.CreatedBy);
            }

            return servicesResponse;
        }

        public async Task<ServiceResponse<NoteResponse>> UpdateNote(Guid noteId, Guid personId, Guid updatedBy, Guid searchFirmId,
                                             string title, string description, DateTimeOffset updatedDate)
        {

            var servicesResponse = new ServiceResponse<NoteResponse>() { Value = new NoteResponse() };

            var note = await _noteRepository.GetById(searchFirmId, noteId);

            note.NoteTitle = title;
            note.NoteDescription = description;
            note.UpdatedBy = updatedBy;
            note.UpdatedDate = updatedDate;

            var validationResults = note.Validate();

            if (validationResults.Any())
            {
                servicesResponse.ValidationErrors = validationResults;
            }
            else
            {
                servicesResponse.Value.Note = await _noteRepository.Update(note);
                servicesResponse.Value.CreatedBy = await _searchFirmRepository.GetUserById(note.SearchFirmId, note.CreatedBy);
                servicesResponse.Value.UpdatedBy = await _searchFirmRepository.GetUserById(note.SearchFirmId, note.UpdatedBy.Value);
            }

            return servicesResponse;
        }

        public async Task<ServiceResponse<List<NoteResponse>>> GetNotes(Guid searchFirmId, List<Guid> noteIds)
        {
            var serviceResponse = new ServiceResponse<List<NoteResponse>>()
            {
                Value = new List<NoteResponse>()
            };

            if (noteIds == null || !noteIds.Any())
            {
                serviceResponse.Value = null;
            }
            else
            {
                var notes = await _noteRepository.GetNotesByIds(searchFirmId, noteIds);

                if (notes != null)
                {
                    foreach (var note in notes.OrderByDescending(n => n.UpdatedDate ?? n.CreatedDate))
                    {
                        serviceResponse.Value.Add(new NoteResponse()
                        {
                            Note = note,
                            CreatedBy = await _searchFirmRepository.GetUserById(note.SearchFirmId, note.CreatedBy),
                            UpdatedBy = note.UpdatedBy.HasValue
                                                        ? await _searchFirmRepository.GetUserById(note.SearchFirmId, note.UpdatedBy.Value)
                                                        : default
                        });
                    }
                }
            }
            return serviceResponse;
        }

    }
}
