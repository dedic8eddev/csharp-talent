using AutoMapper;
using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Application.Services.Assignment.Models;
using Ikiru.Parsnips.Application.Services.DataPool;
using Ikiru.Parsnips.Application.Services.Notes.Models;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.Domain.Notes;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ApiException;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ResourceNotFound;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure;
using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi;
using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Application.Services.Assignment
{
    public class AssignmentService : IAssignmentService
    {
        private readonly AssignmentRepository _assignmentRepository;
        private readonly PortalUserRepository _portalUserRepository;
        private readonly CandidateRepository _candidateRepository;
        private readonly PersonRepository _personRepository;
        private readonly NoteRepository _noteRepository;
        private readonly INoteService _noteService;
        private readonly IMapper _mapper;
        private readonly IIdentityAdminApi _identityAdminApi;
        private readonly IDataPoolService _dataPoolService;
        private readonly ILogger<AssignmentService> _logger;

        private static readonly Random _random = new Random();

        public AssignmentService(AssignmentRepository assignmentRepository, PortalUserRepository portalUserRepository,
                                 INoteService noteService,
                                 IMapper mapper,
                                 IIdentityAdminApi identityAdminApi, ILogger<AssignmentService> logger,
                                 CandidateRepository candidateRepository, PersonRepository personRepository, NoteRepository noteRepository, IDataPoolService dataPoolService)
        {
            _noteService = noteService;
            _assignmentRepository = assignmentRepository;
            _mapper = mapper;
            _identityAdminApi = identityAdminApi;
            _logger = logger;
            _portalUserRepository = portalUserRepository;
            _candidateRepository = candidateRepository;
            _personRepository = personRepository;
            _noteRepository = noteRepository;
            _dataPoolService = dataPoolService;
        }

        public async Task<ServiceResponse<NoteResponse>> CreateAssignmentNote(Guid assignmentId, Guid createdBy, Guid searchFirmId,
                                                                              string title, string description, DateTimeOffset createdDate)
        {
            var servicesResponse = new ServiceResponse<NoteResponse>();

            var note = await _noteService.CreateNote(assignmentId: assignmentId,
                                                     personId: Guid.Empty,
                                                     createdBy: createdBy, searchFirmId: searchFirmId,
                                                     title: title, description: description,
                                                     createdDate: createdDate);

            if (note.ValidationErrors.Any())
            {
                servicesResponse.ValidationErrors = note.ValidationErrors;
                return servicesResponse;
            }

            var assignment = await _assignmentRepository.GetById(searchFirmId, assignmentId);

            if (assignment == null)
            {
                servicesResponse.AddCustomValidationError($"Assignmentid {assignmentId} does note exist", nameof(assignmentId));
            }
            else
            {
                assignment.Notes ??= new List<Guid>();

                assignment.Notes.Add(note.Value.Note.Id);
                await _assignmentRepository.Update(assignment);

                servicesResponse.Value = note.Value;
            }

            return servicesResponse;
        }

        public async Task<ServiceResponse<NoteResponse>> UpdateAssignmentNote(Guid assignmentId, Guid noteId, Guid personId, Guid updatedBy, Guid searchFirmId,
                                                                            string title, string description)
        {
            var servicesResponse = new ServiceResponse<NoteResponse>();

            var assignment = await _assignmentRepository.GetById(searchFirmId, assignmentId);

            if (assignment == null)
            {
                servicesResponse.AddCustomValidationError($"Assignmentid '{assignmentId}' does not exist", nameof(assignmentId));
            }
            else if (assignment.Notes == null || !assignment.Notes.Contains(noteId))
            {
                servicesResponse.AddCustomValidationError($"Assignmentid '{assignmentId}' does not contain note '{noteId}'", nameof(noteId));
            }
            else
            {
                DateTimeOffset updatedDate = DateTime.Now;
                var note = await _noteService.UpdateNote(noteId: noteId, personId: personId, updatedBy: updatedBy, updatedDate: updatedDate,
                                                         searchFirmId: searchFirmId, title: title, description: description);

                if (note.ValidationErrors.Any())
                {
                    servicesResponse.ValidationErrors = note.ValidationErrors;
                }
                else
                {
                    servicesResponse.Value = note.Value;
                }
            }

            return servicesResponse;
        }

        public async Task<ShareAssignmentResultModel> Share(ShareAssignmentCommand command)
        {
            var assignment = await _assignmentRepository.GetById(command.SearchFirmId, command.AssignmentId);
            if (assignment == null)
                throw new ResourceNotFoundException(nameof(Domain.Assignment), command.AssignmentId.ToString());

            var result = new ShareAssignmentResultModel
            {
                Email = command.Email
            };

            string password = null;

            var portalUser = await _portalUserRepository.GetUser(command.SearchFirmId, command.Email);
            if (portalUser != null)
            {
                if (portalUser.SharedAssignments.Any(a => a.AssignmentId == command.AssignmentId))
                {
                    result.UserName = portalUser.UserName;
                    return result;
                }
            }
            else
            {
                password = GenerateRandomPassword();
                var createRequest = new CreateUserRequest
                {
                    SearchFirmId = command.SearchFirmId,
                    EmailAddress = command.Email,
                    Password = password,
                    IsDisabled = false,
                    BypassConfirmEmailAddress = true,
                    GenerateUniqueUserName = true
                };

                CreateUserResult createUserResult;
                try
                {
                    createUserResult = await _identityAdminApi.CreateUser(createRequest);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Creating user in identity throw an exception");
                    throw new ExternalApiException("Authentication", $"External error. Access has not been granted to {command.Email}. Please try again later.");
                }

                if (createUserResult == null)
                    throw new ExternalApiException("Authentication", $"External error. Access has not been granted to {command.Email}. Please try again later.");

                portalUser = new Domain.PortalUser(command.SearchFirmId)
                {
                    IdentityServerId = createUserResult.Id,
                    Email = command.Email,
                    UserName = createUserResult.UserName
                };
            }

            portalUser.SharedAssignments.Add(new PortalSharedAssignment(command.AssignmentId, command.UserId));

            var validationResults = portalUser.Validate();
            if (validationResults.Any())
                throw new ParamValidationFailureException(validationResults);

            portalUser = await _portalUserRepository.Upsert(portalUser);

            result.UserName = portalUser.UserName;
            result.Password = password;

            return result;
        }

        public async Task<GetSharedResultModel> GetShared(GetSharedAssignmentCommand command)
        {
            var assignment = await _assignmentRepository.GetById(command.SearchFirmId, command.AssignmentId);
            if (assignment == null)
                throw new ResourceNotFoundException(nameof(Domain.Assignment), command.AssignmentId.ToString());

            var portalUsers = await _assignmentRepository.GetShared(command.SearchFirmId, command.AssignmentId);

            return new GetSharedResultModel { PortalUsers = _mapper.Map<List<PortalUserModel>>(portalUsers) };
        }

        public async Task Delete(UnshareAssignmentCommand command)
        {
            var assignment = await _assignmentRepository.GetById(command.SearchFirmId, command.AssignmentId);
            if (assignment == null)
                throw new ResourceNotFoundException(nameof(Domain.Assignment), command.AssignmentId.ToString());

            var portalUser = await _portalUserRepository.GetUser(command.SearchFirmId, command.Email);
            var sharedAssignment = portalUser?.SharedAssignments.SingleOrDefault(a => a.AssignmentId == command.AssignmentId);
            if (sharedAssignment == null)
                return;

            portalUser.SharedAssignments.Remove(sharedAssignment);
            await _portalUserRepository.Upsert(portalUser);
        }

        private string GenerateRandomPassword()
        {
            const string alphabet = "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-";
            const int passwordLength = 10;

            var passwordBuilder = new char[passwordLength];
            for (var i = 0; i < passwordLength; ++i)
            {
                var c = alphabet[_random.Next(alphabet.Length - 1)];
                passwordBuilder[i] = c;
            }

            return new string(passwordBuilder);
        }

        public async Task<ServiceResponse<List<NoteResponse>>> GetAllNotesForAssignment(Guid searchFirmId, Guid assignmentId)
        {
            var servicesResponse = new ServiceResponse<List<NoteResponse>>();

            var assignment = await _assignmentRepository.GetById(searchFirmId, assignmentId);

            if (assignment == null)
            {
                servicesResponse.AddCustomValidationError($"Assignmentid {assignmentId} does note exist", nameof(assignmentId));
                return servicesResponse;
            }

            return await _noteService.GetNotes(searchFirmId, assignment.Notes);
        }


        public async Task<ServiceResponse<ActiveAssignmentSimpleResponse>> GetSimple(Guid searchFirmId, int? totalItemCount)
        {
            var result = new ServiceResponse<ActiveAssignmentSimpleResponse>()
            {
                Value = new ActiveAssignmentSimpleResponse()
            };

            var assignments = await _assignmentRepository.GetLatestActiveAssignments(searchFirmId, totalItemCount);

            if (assignments != null && assignments.Any())
            {
                result.Value.SimpleActiveAssignments = _mapper.Map<List<SimpleActiveAssignment>>(assignments);
            }
            else
            {
                result.Value.HasAssignments = await _assignmentRepository.HasAssignments(searchFirmId);
            }

            return result;
        }

        public async Task<ListSharedAssignmentDetailsModel> GetSharedAssignmentsForClient(Guid searchFirmId, Guid identityServerId)
        {
            var listSharedAssignmentDetailsModel = new ListSharedAssignmentDetailsModel()
            {
                SharedAssignmentDetails = new List<SharedAssignmentDetailsModel>()
            };

            var portalUser = await _portalUserRepository.GetUserByIdentityId(searchFirmId, identityServerId);

            if (portalUser == null)
            {
                throw new ParamValidationFailureException("clientEmail and/or searchFirmId", "portal assignment does not exists");
            }

            listSharedAssignmentDetailsModel.ClientEmail = portalUser.Email;


            var assignmentIds = portalUser.SharedAssignments.Select(x => x.AssignmentId).ToList();

            var assignments = await _assignmentRepository.GetAssignmentsByIds(searchFirmId, assignmentIds);


            if (!portalUser.SharedAssignments.Any())
            {
                throw new ParamValidationFailureException("clientEmail and/or searchFirmId", "no portal assignments shared");
            }

            foreach (var assignment in assignments)
            {
                var candidates = await _candidateRepository.GetAllForAssignment(searchFirmId, assignment.Id);

                var sharedAssignmentDetails = new SharedAssignmentDetailsModel()
                {
                    AssignmentId = assignment.Id,
                    Location = assignment.Location,
                    Name = assignment.Name,
                    CompanyName = assignment.CompanyName
                };

                AssignmentCandidateStatus(sharedAssignmentDetails, candidates.Where(x => x.ShowInClientView).ToList());

                listSharedAssignmentDetailsModel.SharedAssignmentDetails.Add(sharedAssignmentDetails);
            }

            return listSharedAssignmentDetailsModel;
        }

        public async Task<GetSharedAssignmentResult> GetSharedAssignmentForPortalUser(GetSharedAssignmentDetailsCommand command)
        {
            var assignment = await _assignmentRepository.GetById(command.SearchFirmId, command.AssignmentId);
            if (assignment == null)
                throw new ResourceNotFoundException(nameof(Domain.Assignment), command.AssignmentId.ToString());

            var portalUser = await _portalUserRepository.GetUserByIdentityId(command.SearchFirmId, command.IdentityServerId);
            if (!portalUser.SharedAssignments.Any(a => a.AssignmentId == command.AssignmentId))
                throw new ResourceNotFoundException(nameof(Domain.Assignment), command.AssignmentId.ToString());

            var result = _mapper.Map<GetSharedAssignmentResult>(assignment);

            var candidates = await _candidateRepository.GetSharedInPortalForAssignment(command.SearchFirmId, command.AssignmentId);
            if (candidates?.Any() == false)
                return result;

            result.Candidates = await LoadCandidates(command.SearchFirmId, candidates);

            return result;
        }

        private async Task<GetSharedAssignmentResult.CandidatesResult> LoadCandidates(Guid searchFirmId, List<Domain.Candidate> candidates)
        {
            var result = new GetSharedAssignmentResult.CandidatesResult();
            result.Candidates = new List<GetSharedAssignmentResult.Candidate>();

            var personIds = candidates.Select(c => c.PersonId).ToList();
            var persons = await _personRepository.GetByIds(searchFirmId, personIds);

            foreach (var candidate in candidates)
            {
                var portalCandidate = _mapper.Map<GetSharedAssignmentResult.Candidate>(candidate);

                portalCandidate.LinkPerson = new GetSharedAssignmentResult.LinkPerson();

                var person = persons.Single(p => p.Id == candidate.PersonId);
                portalCandidate.LinkPerson.LocalPerson = _mapper.Map<GetSharedAssignmentResult.Person>(person);

                if (person.DataPoolPersonId != null && person.DataPoolPersonId != Guid.Empty)
                {
                    var datapoolPerson = await _dataPoolService.GetSinglePersonById(person.DataPoolPersonId.ToString(), CancellationToken.None);

                    var datapoolPersonMapped = _mapper.Map<GetSharedAssignmentResult.Person>(datapoolPerson);
                    portalCandidate.LinkPerson.DataPoolPerson = datapoolPersonMapped;
                }

                if (candidate.SharedNoteId != null)
                {
                    var note = await _noteRepository.GetById(searchFirmId, candidate.SharedNoteId.Value);
                    portalCandidate.LinkSharedNote = _mapper.Map<GetSharedAssignmentResult.Note>(note);
                }
                result.Candidates.Add(portalCandidate);
            }

            return result;
        }

        private void AssignmentCandidateStatus(SharedAssignmentDetailsModel sharedAssignmentDetails, List<Candidate> candidates)
        {
            sharedAssignmentDetails.AssignmentCandidateStageCount = new AssignmentCandidateStageCountModel();

            sharedAssignmentDetails.AssignmentCandidateStageCount.Identified = candidates.Count(x => x.InterviewProgressState.Stage == CandidateStageEnum.Identified);
            sharedAssignmentDetails.AssignmentCandidateStageCount.Screening = candidates.Count(x => x.InterviewProgressState.Stage == CandidateStageEnum.Screening);
            sharedAssignmentDetails.AssignmentCandidateStageCount.InternalInterview = candidates.Count(x => x.InterviewProgressState.Stage == CandidateStageEnum.InternalInterview);
            sharedAssignmentDetails.AssignmentCandidateStageCount.ShortList = candidates.Count(x => x.InterviewProgressState.Stage == CandidateStageEnum.ShortList);
            sharedAssignmentDetails.AssignmentCandidateStageCount.FirstClientInterview = candidates.Count(x => x.InterviewProgressState.Stage == CandidateStageEnum.FirstClientInterview);
            sharedAssignmentDetails.AssignmentCandidateStageCount.SecondClientInterview = candidates.Count(x => x.InterviewProgressState.Stage == CandidateStageEnum.SecondClientInterview);
            sharedAssignmentDetails.AssignmentCandidateStageCount.ThirdClientInterview = candidates.Count(x => x.InterviewProgressState.Stage == CandidateStageEnum.ThirdClientInterview);
            sharedAssignmentDetails.AssignmentCandidateStageCount.Offer = candidates.Count(x => x.InterviewProgressState.Stage == CandidateStageEnum.Offer);
            sharedAssignmentDetails.AssignmentCandidateStageCount.Placed = candidates.Count(x => x.InterviewProgressState.Stage == CandidateStageEnum.Placed);
            sharedAssignmentDetails.AssignmentCandidateStageCount.Archive = candidates.Count(x => x.InterviewProgressState.Stage == CandidateStageEnum.Archive);
        }

        public async Task<AssignmentNote> CreateNote(Guid searchFirmId,
                                                    Guid createdByUserId,
                                                    Guid assignmentId,
                                                    string title,
                                                    string text,
                                                    NoteTypeEnum type,
                                                    ContactMethodEnum contactMethod)
        {
            var assignmentNote = new AssignmentNote(searchFirmId,
                                            createdByUserId,
                                            assignmentId,
                                            title,
                                            text,
                                            type,
                                            contactMethod);

            assignmentNote.Validate();

            await _noteRepository.CreateAssignmentNote(assignmentNote);

            return assignmentNote;
        }

        public async Task<AssignmentNote> GetNoteById(Guid noteId)
        {
            var a = await _noteRepository.GetAssignmentNoteById(noteId);

            return a;
        }

        public async Task<AssignmentNote> UpdateNote(Guid id, string title,
                                                string text, NoteTypeEnum type, ContactMethodEnum contactMethod,
                                                DateTimeOffset lastEdited, Guid lastEditedBy, bool pinned)
        {
            var assignmentNote = await GetNoteById(id);

            if (assignmentNote == null)
            {
                throw new Exception($"Assignment note not found with id: {id}");
            }

            assignmentNote.Update(title: title,
                                    text: text,
                                    type: type,
                                    contactMethod: contactMethod,
                                    pinned: pinned,
                                    lastEdited: lastEdited,
                                    lastEditedBy: lastEditedBy);


            var validationResults = assignmentNote.Validate();

            if (validationResults.Any())
            {
                throw new Exception(validationResults.Select(x => $"{x.MemberNames}{x.ErrorMessage}").ToString());
            }

            await _noteRepository.UpdateAssignmentNote(assignmentNote);

            return assignmentNote;
        }
    }
}