using AutoMapper;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.DomainInfrastructure;
using Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi;
using Microsoft.Azure.Cosmos.Linq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Services.ExportCandidates
{
    public class CandidatesFetcher
    {
        private readonly DataPoolPersonFetcher m_DataPoolPersonFetcher;
        private readonly DataQuery m_DataQuery;
        private readonly IMapper m_Mapper;

        public CandidatesFetcher(DataPoolPersonFetcher dataPoolPersonFetcher,DataQuery dataQuery, IMapper mapper)
        {
            m_DataPoolPersonFetcher = dataPoolPersonFetcher; 
            m_DataQuery = dataQuery;
            m_Mapper = mapper;
        }

        public class Candidate
        {
            public string Name { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Location { get; set; }
            public string JobTitle { get; set; }
            public string Organisation { get; set; }
            public string EmailAddresses { get; set; }
            public string PhoneNumbers { get; set; }
            public string LinkedInProfileUrl { get; set; }
            public CandidateStageEnum? Stage { get; set; }
            public CandidateStatusEnum? Status { get; set; }
        }

        public async Task<Candidate[]> FetchForExport(Guid searchFirmId, Guid assignmentId, CancellationToken cancellationToken,
                                                    Guid[] candidateIds = null)
        {
            if (!await AssignmentExists(searchFirmId, assignmentId, cancellationToken))
                return new Candidate[0];

            var filter = new Func<IOrderedQueryable<Domain.Candidate>, IQueryable<Domain.Candidate>>
                (q => q.Where(c => c.AssignmentId == assignmentId));

            var candidates = await m_DataQuery.FetchAllItems<Domain.Candidate>(searchFirmId.ToString(), filter, cancellationToken);

            if (candidateIds != null)
            {
                candidates = candidates.Where(c => candidateIds.Contains(c.Id)).ToList();
            }

            var personIds = candidates.Select(c => c.PersonId).ToList();
            var personsFilter = new Func<IOrderedQueryable<Domain.Person>, IQueryable<Domain.Person>>
                (q => q.Where(p => personIds.Contains(p.Id)));

            var persons = await m_DataQuery.FetchAllItems<Domain.Person>(searchFirmId.ToString(), personsFilter, cancellationToken);

            var candidatesToExport = new Candidate[persons.Count];

            for (var i = 0; i < persons.Count; ++i)
            {
                var person = persons[i];

                person = await MergeInDataPoolPerson(person, cancellationToken);

                var candidate = candidates.Single(c => c.PersonId == person.Id);

                var currentCandidate = m_Mapper.Map<Candidate>(person);
                currentCandidate.Status = candidate.InterviewProgressState?.Status;
                currentCandidate.Stage = candidate.InterviewProgressState?.Stage;

                SetFirstLastName(currentCandidate);
                candidatesToExport[i] = currentCandidate;
            }

            return candidatesToExport;
        }

        private async Task<Domain.Person> MergeInDataPoolPerson(Domain.Person person, CancellationToken cancellationToken)
        {
            if (person.DataPoolPersonId == null)
            {
                return person;
            }

            Shared.Infrastructure.DataPoolApi.Models.Person.Person dataPoolResponse;
            try
            {
                dataPoolResponse = await m_DataPoolPersonFetcher.GetPersonById(person.DataPoolPersonId.ToString(), cancellationToken);
            }
            catch
            {
                return person;
            }

            if (dataPoolResponse == null)
                return person;

            person = MergePerson(person, dataPoolResponse);

            return person;
        }

        private Domain.Person MergePerson(Domain.Person person, Shared.Infrastructure.DataPoolApi.Models.Person.Person dataPoolResponse)
        {
            var location =
                dataPoolResponse.Location == null
                    ? ""
                    : ExtractLocationString.FromDataPoolLocation(dataPoolResponse.Location);

            var linkedInUrl = dataPoolResponse
                             .WebsiteLinks
                             .FirstOrDefault(w => w.LinkTo == Shared.Infrastructure.DataPoolApi.Models.Common.Linkage.LinkedInProfile)
                            ?.Url ?? "";

            person.Name = string.IsNullOrEmpty(person.Name) ? dataPoolResponse.PersonDetails.Name ?? "" : person.Name;
            person.Location = string.IsNullOrEmpty(person.Location) ? location : person.Location;
            person.Organisation = string.IsNullOrEmpty(person.Organisation) ? dataPoolResponse.CurrentEmployment.CompanyName ?? "" : person.Organisation;
            person.JobTitle = string.IsNullOrEmpty(person.JobTitle) ? dataPoolResponse.CurrentEmployment.Position ?? "" : person.JobTitle;
            person.LinkedInProfileUrl = string.IsNullOrEmpty(person.LinkedInProfileUrl) ? linkedInUrl : person.LinkedInProfileUrl;
            return person;
        }

        private static void SetFirstLastName(Candidate candidate)
        {
            var name = candidate.Name;
            if (string.IsNullOrWhiteSpace(name))
                return;

            name = name.Trim();

            var spaceIndex = name.IndexOf(" ", StringComparison.InvariantCulture);
            if (spaceIndex >= 0)
            {
                candidate.FirstName = name.Substring(0, spaceIndex);
                candidate.LastName = name.Substring(spaceIndex + 1, name.Length - spaceIndex - 1);
            }
            else
            {
                candidate.FirstName = name;
            }
        }

        private async Task<bool> AssignmentExists(Guid searchFirmId, Guid assignmentId, CancellationToken cancellationToken)
        {
            var feedIterator = await m_DataQuery
                                    .GetItemLinqQueryable<Assignment>(searchFirmId.ToString())
                                    .Where(c => c.Id == assignmentId)
                                    .CountAsync(cancellationToken);

            return feedIterator.Resource == 1;
        }
    }
}
