using Ikiru.Parsnips.Domain.Base;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Ikiru.Parsnips.Domain
{
    public class Candidate : MultiTenantedDomainObject
    {
        public Guid AssignmentId { get; }
        public Guid PersonId { get; }
        
        [Required]
        public InterviewProgress InterviewProgressState { get; set; }

        public Guid? AssignTo { get; set; }
        public DateTimeOffset? DueDate { get; set; }
        public bool ShowInClientView { get; set; }
        public Guid? SharedNoteId { get; set; }

        /* Serialiser Constructor */
        [JsonConstructor]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Serialisation Ctor")]
        private Candidate(Guid id, DateTimeOffset createdDate, Guid searchFirmId, Guid assignmentId, Guid personId) : base(id, createdDate, searchFirmId)
        {
            AssignmentId = assignmentId;
            PersonId = personId;
        }

        /* Business Logic Constructor */
        public Candidate(Guid searchFirmId, Guid assignmentId, Guid personId) : base(searchFirmId)
        {
            AssignmentId = assignmentId;
            PersonId = personId;
        }

    }
}
