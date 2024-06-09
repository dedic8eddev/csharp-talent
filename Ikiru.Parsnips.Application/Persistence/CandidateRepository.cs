using Ikiru.Parsnips.Domain;
using Ikiru.Persistence.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Application.Persistence
{
    public class CandidateRepository : RepositoryBase<Candidate>
    {
        private readonly IRepository _repository;

        public CandidateRepository(IRepository persistenceService) : base(persistenceService)
        {
            _repository = persistenceService;
        }

        public Task<List<Candidate>> GetAllForAssignment(Guid searchFirmId, Guid assignmentId)
            => _repository.GetByQuery<Candidate, Candidate>(searchFirmId.ToString(), i => i.Where(c => c.AssignmentId == assignmentId));

        public Task<List<Candidate>> GetSharedInPortalForAssignment(Guid searchFirmId, Guid assignmentId)
            => _repository.GetByQuery<Candidate, Candidate>(searchFirmId.ToString(), i => i.Where(c => c.AssignmentId == assignmentId && c.ShowInClientView));
    }
}
