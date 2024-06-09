using AutoMapper;
using Ikiru.Parsnips.Application.Services.Assignment;
using Ikiru.Parsnips.Application.Services.Assignment.Models;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ResourceNotFound;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure;
using Ikiru.Parsnips.UnitTests.Helpers;
using Morcatko.AspNetCore.JsonMergePatch;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Application.Services.Assignment
{
    public class CandidateServicesTests
    {
        private readonly Guid _candidateId;
        private readonly Guid _searchFirmId = Guid.NewGuid();
        private readonly Guid _assignmentId = Guid.NewGuid();
        private readonly Guid _personId = Guid.NewGuid();
        private readonly DateTimeOffset _dueDate = DateTimeOffset.UtcNow.AddDays(3);

        private readonly FakeRepository _repository = new FakeRepository();
        
        private Dictionary<string, object> _patchCandidateCommand = new Dictionary<string, object>(); //dictionary as we need structure with properties missing
        private readonly Candidate _candidate;

        public CandidateServicesTests()
        {
            _candidate = new Candidate(_searchFirmId, _assignmentId, _personId)
            {
                DueDate = _dueDate
            };
            _candidateId = _candidate.Id;

            var user = new SearchFirmUser(_searchFirmId);

            _patchCandidateCommand["AssignTo"] = user.Id;
            _patchCandidateCommand["InterviewProgressState"] = new PatchCandidateModel.InterviewProgress();

            _repository.AddToRepository(_candidate, user);
        }

        [Fact]
        public async Task ServicePatchesCandidate()
        {
            // Arrange
            var interviewProgress = (PatchCandidateModel.InterviewProgress)_patchCandidateCommand["InterviewProgressState"];
            interviewProgress.Status = Domain.Enums.CandidateStatusEnum.AwaitingReferences;
            interviewProgress.Stage = Domain.Enums.CandidateStageEnum.ThirdClientInterview;
            _patchCandidateCommand["ShowInClientView"] = true;
            _patchCandidateCommand["SharedNoteId"] = Guid.NewGuid();
            var service = CreateService();

            // Act
            await service.PatchCandidate(_searchFirmId, _candidateId, CreateCommand());

            // Assert
            var result = await _repository.GetItem<Candidate>(_searchFirmId.ToString(), _candidateId.ToString());
            Assert.Equal(_assignmentId, result.AssignmentId);
            Assert.Equal(_personId, result.PersonId);
            Assert.Equal(interviewProgress.Stage, result.InterviewProgressState.Stage);
            Assert.Equal(interviewProgress.Status, result.InterviewProgressState.Status);
            Assert.Equal(_patchCandidateCommand["AssignTo"], result.AssignTo);
            Assert.Equal(_dueDate, result.DueDate); //remains unchanged as not present in the command
            Assert.Equal(_patchCandidateCommand["ShowInClientView"], result.ShowInClientView);
            Assert.Equal(_patchCandidateCommand["SharedNoteId"], result.SharedNoteId);
        }

        public static IEnumerable<object[]> PatchCandidateNoteTestData()
        {
            yield return new object[] { Guid.NewGuid() };
            yield return new object[] { null };
        }

        [Theory]
        [MemberData(nameof(PatchCandidateNoteTestData))]
        public async Task ServicePatchesCandidateNote(Guid? SharedNoteId)
        {
            // Arrange
            _patchCandidateCommand["SharedNoteId"] = SharedNoteId;
            var service = CreateService();

            // Act
            await service.PatchCandidate(_searchFirmId, _candidateId, CreateCommand());

            // Assert
            var storedAssignment = await _repository.GetItem<Candidate>(_searchFirmId.ToString(), _candidateId.ToString());
            Assert.Equal(_assignmentId, storedAssignment.AssignmentId);
            Assert.Equal(_personId, storedAssignment.PersonId);
            Assert.Equal(SharedNoteId, storedAssignment.SharedNoteId);
        }

        [Fact]
        public async Task ServiceReturnsCandidate()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = await service.PatchCandidate(_searchFirmId, _candidateId, CreateCommand());

            // Assert
            var candidate = await _repository.GetItem<Candidate>(_searchFirmId.ToString(), _candidateId.ToString());

            Assert.Equal(candidate.AssignmentId, result.AssignmentId);
            Assert.Equal(candidate.PersonId, result.PersonId);
            Assert.Equal(candidate.InterviewProgressState.Stage, result.InterviewProgressState.Stage);
            Assert.Equal(candidate.InterviewProgressState.Status, result.InterviewProgressState.Status);
            Assert.Equal(candidate.AssignTo, result.AssignTo);
            Assert.Equal(candidate.DueDate, result.DueDate);
            Assert.Equal(candidate.ShowInClientView, result.ShowInClientView);
            Assert.Equal(candidate.SharedNoteId, result.SharedNoteId);
        }

        [Fact]
        public async Task ServiceThrowsWhenNoCommand()
        {
            // Arrange
            var service = CreateService();

            // Act
            var ex = await Record.ExceptionAsync(() => service.PatchCandidate(_searchFirmId, _candidateId, null));

            // Assert
            Assert.NotNull(ex);
            Assert.IsType<ParamValidationFailureException>(ex);
        }

        [Theory, CombinatorialData]
        public async Task ServiceThrowsWhenNoCandidate(bool searchFirmMissing)
        {
            // Arrange
            var searchFirmId = searchFirmMissing ? Guid.NewGuid() : _searchFirmId;
            var candidateId = searchFirmMissing ? _candidateId : Guid.NewGuid();
            var service = CreateService();

            // Act
            var ex = await Record.ExceptionAsync(() => service.PatchCandidate(searchFirmId, candidateId, CreateCommand()));

            // Assert
            Assert.NotNull(ex);
            Assert.IsType<ResourceNotFoundException>(ex);
        }

        [Fact]
        public async Task ServiceThrowsWhenWrongAssignTo()
        {
            // Arrange
            _patchCandidateCommand["AssignTo"] = Guid.NewGuid();
            var service = CreateService();

            // Act
            var ex = await Record.ExceptionAsync(() => service.PatchCandidate(_searchFirmId, _candidateId, CreateCommand()));

            // Assert
            Assert.NotNull(ex);
            Assert.IsType<ParamValidationFailureException>(ex);
        }

        [Fact]
        public async Task ServiceThrowsWhenValidationFails()
        {
            // Arrange
            _patchCandidateCommand["InterviewProgressState"] = null;
            var service = CreateService();

            // Act
            var ex = await Record.ExceptionAsync(() => service.PatchCandidate(_searchFirmId, _candidateId, CreateCommand()));

            // Assert
            Assert.NotNull(ex);
            Assert.IsType<ParamValidationFailureException>(ex);
        }

        private JsonMergePatchDocument<PatchCandidateModel> CreateCommand()
            => PatchCommandHelper.CreatePatchDocument<PatchCandidateModel>(_patchCandidateCommand);

        private CandidateServices CreateService()
        {
            var config = new MapperConfiguration(cfg => cfg.AddProfile<Ikiru.Parsnips.Application.MappingProfile>());
            var mapper = config.CreateMapper();

            return new ServiceBuilder<CandidateServices>()
                  .SetFakeRepository(_repository)
                  .AddTransient(mapper)
                  .Build();
        }
    }
}