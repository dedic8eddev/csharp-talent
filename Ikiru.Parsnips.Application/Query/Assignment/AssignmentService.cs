using AutoMapper;
using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Application.Query.Assignment.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Application.Query.Assignment
{
    public interface IAssignmentService
    {
        Task<ActiveAssignmentSimpleResponse> GetSimple(Guid searchFirmId, int? totalItemCount);
    }

    public class AssignmentService : IAssignmentService
    {
        private readonly AssignmentRepository _assignmentRepository;
        private readonly IMapper _mapper;

        public AssignmentService(AssignmentRepository assignmentRepository, IMapper mapper)
        {
            _assignmentRepository = assignmentRepository;
            _mapper = mapper;
        }

        public async Task<ActiveAssignmentSimpleResponse> GetSimple(Guid searchFirmId, int? totalItemCount)
        {
            var result = new ActiveAssignmentSimpleResponse();

            var assignments = await _assignmentRepository.GetLatestActiveAssignments(searchFirmId, totalItemCount);
            
            if (assignments != null && assignments.Any())
            {
                result.SimpleActiveAssignments = _mapper.Map<List<SimpleActiveAssignment>>(assignments);
            }
            else
            {
                result.HasAssignments = await _assignmentRepository.HasAssignments(searchFirmId);
            }

            return result;
        }
    }
}
