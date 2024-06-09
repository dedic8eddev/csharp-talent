using Ikiru.Parsnips.Domain.Enums;
using System;
using System.Collections.Generic;

namespace Ikiru.Parsnips.Application.Services.Assignment.Models
{
    public class GetSharedAssignmentResult
    {
        public string Name { get; set; }
        public string CompanyName { get; set; }
        public string JobTitle { get; set; }
        public string Location { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public AssignmentStatus Status { get; set; }
        public CandidatesResult Candidates { get; set; }

        public class CandidatesResult
        {
            public List<Candidate> Candidates { get; set; }
            //public bool HasMoreResults { get; set; }
            //public int Count { get; set; }

        }
        public class LinkPerson
        {
            public Person LocalPerson { get; set; }
            public Person DataPoolPerson { get; set; }
        }

        public class Candidate
        {
            public InterviewProgress InterviewProgressState { get; set; }
            public LinkPerson LinkPerson { get; set; }
            //public Guid? AssignTo { get; set; }
            public DateTimeOffset? DueDate { get; set; }
            public Note LinkSharedNote { get; set; }
        }

        public class Note
        {
            public string NoteTitle { get; set; }
            public string NoteDescription { get; set; }
        }

        public class InterviewProgress
        {
            public CandidateStageEnum Stage { get; set; }
            public CandidateStatusEnum Status { get; set; }
        }


        public class Job
        {
            public string CompanyName { get; set; }
            public string Position { get; set; }
            public DateTimeOffset? StartDate { get; set; }
            public DateTimeOffset? EndDate { get; set; }
        }

        public class PersonWebsite
        {
            public string Url { get; set; }
            public WebSiteType Type { get; set; }

        }
        public class Person
        {
            public Guid? Id { get; set; }
            public Guid? DataPoolId { get; set; }
            public string Name { get; set; }
            public string JobTitle { get; set; }
            public string Company { get; set; }
            public string LinkedInProfileUrl { get; set; }
            public List<PersonWebsite> WebSites { get; set; }
            public Job CurrentJob { get; set; }
            public List<Job> PreviousJobs { get; set; }
            public string Location { get; set; }
        }
    }
}
