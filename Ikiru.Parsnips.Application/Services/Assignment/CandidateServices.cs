using AutoMapper;
using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Application.Services.Assignment.Models;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ResourceNotFound;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure;
using Morcatko.AspNetCore.JsonMergePatch;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Application.Services.Assignment
{
    public class CandidateServices
    {
        private readonly CandidateRepository _candidateRepository;
        private readonly UserRepository _userRepository;
        private readonly IMapper _mapper;

        public CandidateServices(CandidateRepository candidateRepository, UserRepository userRepository, IMapper mapper)
        {
            _candidateRepository = candidateRepository;
            _userRepository = userRepository;
            _mapper = mapper;
        }

        public async Task<PatchResultCandidateModel> PatchCandidate(Guid searchFirmId, Guid candidateId, JsonMergePatchDocument<PatchCandidateModel> command)
        {
            if (command == null || command.Model == null)
                throw new ParamValidationFailureException("Model", "Not provided");

            var candidate = await _candidateRepository.GetById(searchFirmId, candidateId);
            if (candidate == null)
                throw new ResourceNotFoundException(nameof(Candidate));

            await ValidateAssignedUser(searchFirmId, command.Model.AssignTo);

            command.ApplyToT(candidate);

            var validationResults = candidate.Validate();
            if (validationResults.Any())
                throw new ParamValidationFailureException(validationResults);

            await _candidateRepository.Update(candidate);

            return _mapper.Map<PatchResultCandidateModel>(candidate);
        }

        private async Task ValidateAssignedUser(Guid searchFirmId, Guid? assignTo)
        {
            if (assignTo == null)
                return;

            var user = await _userRepository.GetUserById(assignTo.Value, searchFirmId);
            if(user == null)
                throw new ParamValidationFailureException(nameof(user), assignTo.ToString());
        }
    }
}
