using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Persistence.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Application.Persistence
{
    public class AssignmentRepository : RepositoryBase<Assignment>
    {
        private readonly IRepository _repository;

        public AssignmentRepository(IRepository repository) : base(repository)
        {
            _repository = repository;
        }

        public async Task<Assignment> GetAssignmentForCandidate(Guid assignmentId)
        {
            var assignments = await _repository.GetByQuery<Assignment>(a => a.Id == assignmentId);

            return assignments.OrderByDescending(x => x.CreatedDate).FirstOrDefault();
        }

        public Task<List<Assignment>> GetLatestActiveAssignments(Guid searchFirmId, int? totalItemCount)
        {
            return _repository.GetByQuery<Assignment, Assignment>
                                  (searchFirmId.ToString(),
                                   i => i
                                       .Where(a => a.SearchFirmId == searchFirmId && a.Status == AssignmentStatus.Active)
                                       .OrderByDescending(a => a.CreatedDate),
                                   totalItemCount);
        }

        public async Task<bool?> HasAssignments(Guid searchFirmId)
        {
            var assignments = await _repository.GetByQuery<Assignment, Guid>
                                  (searchFirmId.ToString(),
                                   i => i.Where(a => a.SearchFirmId == searchFirmId).Select(a => a.Id), 1);
            return assignments.Any();
        }

        public Task<List<PortalUser>> GetShared(Guid searchFirmId, Guid assignmentId)
            => _repository.GetByQuery<PortalUser, PortalUser>
                                  (searchFirmId.ToString(),
                                   u => u.Where(user => user.Discriminator == nameof(PortalUser) 
                                       && user.SearchFirmId == searchFirmId
                                       && user.SharedAssignments != null
                                       && user.SharedAssignments.Any(a => a.AssignmentId == assignmentId)));

        public Task<List<Assignment>> GetAssignmentsByIds(Guid searchFirmId, List<Guid> assignmentIds)
        {
            return _repository.GetByQuery<Assignment, Assignment>
                                  (searchFirmId.ToString(),
                                   i => i
                                       .Where(a => a.SearchFirmId == searchFirmId && assignmentIds.Contains(a.Id)
                                       // && (a.Status == AssignmentStatus.Active || && a.Status == AssignmentStatus.Placed) //todo: spoke to PM they prefer that "placed is fine but on hold or abandoned is best not to show"
                                       )
                                       .OrderByDescending(a => a.CreatedDate));
        }
    }
}
