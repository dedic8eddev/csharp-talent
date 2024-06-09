using AutoMapper;
using Ikiru.Parsnips.Application.Infrastructure.DataPool;
using Ikiru.Parsnips.Application.Infrastructure.Storage;
using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Application.Services.Person.Models;
using Ikiru.Parsnips.Application.Shared.Models;
using Ikiru.Parsnips.Domain.Notes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Application.Services.Person
{
    public class PersonService : IPersonService, IPersonNoteService
    {
        private readonly NoteRepository _noteRepository;
        private readonly PersonRepository _personRepository;
        private readonly AssignmentRepository _assignmentRepository;
        private readonly SearchFirmRepository _searchFirmRepository;
        private readonly IPersonInfrastructure _personInfrastructure;
        private readonly IStorageInfrastructure _storageInfrastructure;
        private readonly IMapper _mapper;

        public PersonService(
            NoteRepository noteRepository,
            PersonRepository personRepository,
            AssignmentRepository assignmentRepository,
            SearchFirmRepository searchFirmRepository,
            IPersonInfrastructure personInfrastructure,
            IStorageInfrastructure storageInfrastructure,
            IMapper mapper)
        {
            _noteRepository = noteRepository;
            _personRepository = personRepository;
            _assignmentRepository = assignmentRepository;
            _searchFirmRepository = searchFirmRepository;
            _personInfrastructure = personInfrastructure;
            _storageInfrastructure = storageInfrastructure;
            _mapper = mapper;
        }

        public async Task<GetByWebsiteUrlResponse> GetByWebSiteUrl(GetByWebsiteUrlRequest query)
        {


            var response = new GetByWebsiteUrlResponse();

            response.DataPoolPerson = await GetDataPoolByURLResult(query);
            response.LocalPerson = await GetLocalPersonResult(query);
            return response;
        }

        public async Task<SearchPersonQueryResult> SearchPersonByQuery(Models.SearchPersonQueryRequest query)
        {
            var repsonse = new SearchPersonQueryResult();

            var searchQuery = _mapper.Map<Infrastructure.DataPool.Models.SearchPersonQueryRequest>(query);

            var datapoolPersonsSearchResults = await _personInfrastructure.SearchPersons(searchQuery);

            if (datapoolPersonsSearchResults == null)
            {
                return null;
            }

            var searchPersonsQueryResult = _mapper.Map<SearchPersonQueryResult>(datapoolPersonsSearchResults);

            searchPersonsQueryResult.PersonsWithAssignmentIds = new List<PersonWithAssignmentIdsResult>();

            var localPersons = await _personRepository.GetManyLocalPersonsByTheirDatapoolId(datapoolPersonsSearchResults.Results
                                            .Select(dp => dp.Id).ToArray(), query.SearchFirmId);

            foreach (var datapoolPerson in datapoolPersonsSearchResults.Results)
            {
                Guid[] candidateAssignmentIds = new Guid[] { };

                var localPerson = localPersons.Where(p => p.DataPoolPersonId == datapoolPerson.Id).FirstOrDefault();
                var datapoolPersonResult = _mapper.Map<Ikiru.Parsnips.Application.Shared.Models.Person>(datapoolPerson);


                if (localPerson != null)
                {
                    datapoolPersonResult.PersonId = localPerson.Id;

                    candidateAssignmentIds = (await _personRepository.GetAllWherePersonIsCandidate(localPerson.Id, query.SearchFirmId))
                                                                        .Select(x => x.AssignmentId).ToArray();
                    if (localPerson?.WebSites != null)
                    {
                        localPerson.WebSites.ForEach(wl =>
                        {
                            if (!datapoolPersonResult.Websites.Exists(dpw => dpw.Url == wl.Url))
                            {
                                datapoolPersonResult.Websites.Add(new WebsiteLink()
                                {
                                    Url = wl.Url,
                                    WebsiteType = wl.Type
                                });
                            }
                        });
                    }
                }

                if (datapoolPerson?.PersonDetails?.PhotoUrl != null)
                {
                    datapoolPersonResult.Photo = new Photo { Url = datapoolPerson.PersonDetails.PhotoUrl };
                }

                var currentJob = _mapper.Map<PersonJob>(datapoolPerson.CurrentEmployment);
                var previousJobs = _mapper.Map<List<PersonJob>>(datapoolPerson.PreviousEmployment);

                searchPersonsQueryResult.PersonsWithAssignmentIds.Add(new PersonWithAssignmentIdsResult()
                {
                    Person = datapoolPersonResult,
                    AssignmentIds = candidateAssignmentIds,
                    CurrentJob = currentJob,
                    PreviousJobs = previousJobs
                });
            }

            return searchPersonsQueryResult;
        }

        public async Task<PersonNote> CreateNote(Guid searchFirmId, Guid createdBy, Guid personId, Guid assignmentId,
                                            string title, string text, NoteTypeEnum type,
                                            ContactMethodEnum contactMethod)
        {
            var personNote = new PersonNote(searchFirmId: searchFirmId,
                                            createdByUserId: createdBy,
                                            personId: personId,
                                            assignmentId: assignmentId,
                                            title: title,
                                            text: text,
                                            type: type,
                                            contactMethod: contactMethod);

            var validationResults = personNote.Validate();

            if (validationResults.Any())
            {
                var errors = validationResults.Select(x => x.ErrorMessage).ToList();
                var members = validationResults.SelectMany(x => x.MemberNames).ToList();

                var errorsSB = new StringBuilder();

                for (int i = 0; i < members.Count; i++)
                {
                    errorsSB.AppendLine($"Property: {members[i]}, Error: {errors[i]}");
                }

                throw new Exception(errorsSB.ToString());
            }

            await _noteRepository.CreatePersonNote(personNote);

            return personNote;
        }

        public async Task<PersonNote> GetNoteById(Guid noteId)
        {
            return await _noteRepository.GetPersonNoteById(noteId);
        }

        public async Task<PersonNote> UpdateNote(Guid id, Guid personId, Guid assignmentId, string title,
                                                string text, NoteTypeEnum type, ContactMethodEnum contactMethod, DateTimeOffset lastEdited, Guid lastEditedBy)
        {
            var personNote = await GetNoteById(id);


            if (personNote == null)
            {
                throw new Exception($"Person note not found with id: {id}");
            }

            personNote.Update(assignmentId: assignmentId,
                            title: title,
                            text: text,
                            type: type,
                            contactMethod: contactMethod,
                            lastEditedBy: lastEditedBy,
                            lastEdited: lastEdited);

            var validationResults = personNote.Validate();

            if (validationResults.Any())
            {
                throw new Exception(validationResults.Select(x => $"{x.MemberNames}{x.ErrorMessage}").ToString());
            }

            await _noteRepository.UpdatePersonNote(personNote);


            return personNote;
        }

        private async Task<GetDataPoolPersonByWebsiteUrlResponse> GetDataPoolByURLResult(GetByWebsiteUrlRequest query)
        {
            var datapoolPersonResult = new GetDataPoolPersonByWebsiteUrlResponse();

            var datapoolPerson = await _personInfrastructure.GetPersonByWebsiteUrl(query.WebsiteUrl);

            datapoolPersonResult = _mapper.Map<GetDataPoolPersonByWebsiteUrlResponse>(datapoolPerson);

            if (datapoolPerson?.PersonDetails?.PhotoUrl != null)
            {
                datapoolPersonResult.Photo = new Shared.Models.Photo { Url = datapoolPerson.PersonDetails.PhotoUrl };
            }

            datapoolPersonResult?.Websites.Sort((a, b) => a.WebsiteType.CompareTo(b.WebsiteType));

            return datapoolPersonResult;
        }

        public async Task<GetLocalPersonByWebsiteUrlResponse> GetLocalPersonResult(GetByWebsiteUrlRequest query)
        {
            var profileUrl = query.WebsiteUrl;

            List<Ikiru.Parsnips.Domain.Person> localPersonDomain;
            if (profileUrl.ToLower().Contains("linkedin"))
            {
                var linkedinProfileId = Ikiru.Parsnips.Domain.Person.NormaliseLinkedInProfileUrl(profileUrl);

                localPersonDomain = await _personRepository.GetPersonsByLinkedInProfileId(linkedinProfileId, query.SearchFirmId);
            }
            else
            {
                localPersonDomain = await _personRepository.GetPersonsByWebsiteUrl(profileUrl, query.SearchFirmId);
            }

            if (!localPersonDomain.Any())
            {
                return null;
            }

            var localPerson = localPersonDomain.FirstOrDefault();

            GetLocalPersonByWebsiteUrlResponse personResult = null;

            personResult = _mapper.Map<GetLocalPersonByWebsiteUrlResponse>(localPerson);

            var candidates = await _personRepository.GetAllWherePersonIsCandidate(localPerson.Id, query.SearchFirmId);

            if (candidates.Any())
            {
                var candidate = candidates.First();

                var assignment = await _assignmentRepository.GetAssignmentForCandidate(candidate.AssignmentId);

                if (assignment != null && assignment != default)
                {
                    if (personResult?.RecentAssignment == null)
                    {
                        personResult.RecentAssignment = new Shared.Models.Assignment();
                    }

                    personResult.RecentAssignment = new Shared.Models.Assignment
                    {
                        Name = assignment.Name,
                        Stage = candidate.InterviewProgressState.Stage.ToString(),
                        Status = candidate.InterviewProgressState.Status.ToString()
                    };
                }

                var recentNote = await _noteRepository.GetLatestNoteForPerson(localPerson.Id, query.SearchFirmId);

                if (recentNote != null && recentNote != default)
                {
                    personResult.RecentNote = new Shared.Models.Note();

                    if (recentNote.UpdatedDate.HasValue)
                    {
                        if (recentNote.UpdatedBy != null)
                        {
                            var user = await _searchFirmRepository.GetUserById(query.SearchFirmId, recentNote.UpdatedBy.Value);

                            personResult.RecentNote.CreatedOrUpdated = recentNote.UpdatedDate.Value;
                            personResult.RecentNote.ByFirstName = user.FirstName;
                            personResult.RecentNote.ByLastName = user.LastName;
                        }
                    }
                    else
                    {
                        var user = await _searchFirmRepository.GetUserById(query.SearchFirmId, recentNote.CreatedBy);

                        personResult.RecentNote.CreatedOrUpdated = recentNote.CreatedDate;
                        personResult.RecentNote.ByFirstName = user.FirstName;
                        personResult.RecentNote.ByLastName = user.LastName;
                    }

                    personResult.RecentNote.NoteTitle = recentNote.NoteTitle;
                }
            }

            if (localPerson != null)
            {
                var photoUri = await _storageInfrastructure.GetTemporaryUrl(query.SearchFirmId, localPerson.Id, new CancellationToken());
            }

            personResult?.Websites.Sort((a, b) => a.WebsiteType.CompareTo(b.WebsiteType));

            return personResult;
        }
    }
}
