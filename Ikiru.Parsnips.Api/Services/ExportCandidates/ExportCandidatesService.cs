using Ikiru.Parsnips.Api.Authentication;
using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.DomainInfrastructure;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ResourceNotFound;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Services.ExportCandidates
{
    public class ExportCandidatesService
    {
        public class Result
        {
            public byte[] ExportData { get; set; }
            public string FileName { get; set; }
        }

        private readonly AuthenticatedUserAccessor m_AuthenticatedUserAccessor;
        private readonly CandidatesFetcher m_CandidatesFetcher;
        private readonly CsvGenerator m_CsvGenerator;
        private readonly DataQuery m_DataQuery;
        private readonly ILogger<ExportCandidatesService> m_Logger;


        public ExportCandidatesService(AuthenticatedUserAccessor authenticatedUserAccessor, CandidatesFetcher candidatesFetcher,
                                       CsvGenerator csvGenerator, DataQuery dataQuery,
                                       ILogger<ExportCandidatesService> logger)
        {
            m_AuthenticatedUserAccessor = authenticatedUserAccessor;
            m_CandidatesFetcher = candidatesFetcher;
            m_CsvGenerator = csvGenerator;
            m_DataQuery = dataQuery;
            m_Logger = logger;
        }

        public async Task<Result> Generate(Guid assignmentId, CancellationToken cancellationToken, Guid[] candidateIds = null)
        {
            var sw = Stopwatch.StartNew();

            var searchFirmId = m_AuthenticatedUserAccessor.GetAuthenticatedUser().SearchFirmId;
            m_Logger.LogDebug($"Exporting candidate on SearchFirmId '{searchFirmId}', AssignmentId '{assignmentId}', timer: {sw.ElapsedMilliseconds} ms.");

            var assignmentName = await GetAssignmentNameOrThrow(searchFirmId, assignmentId, cancellationToken);

            m_Logger.LogTrace($"Export AssignmentId '{assignmentId}', AssignmentName {assignmentName}, timer: {sw.ElapsedMilliseconds} ms.");

            var candidates = await m_CandidatesFetcher.FetchForExport(searchFirmId, assignmentId, cancellationToken, candidateIds);

            m_Logger.LogTrace($"Export AssignmentId '{assignmentId}', fetched {candidates.Length} candidates, timer: {sw.ElapsedMilliseconds} ms.");

            var exportData = await m_CsvGenerator.Run(candidates, cancellationToken);

            m_Logger.LogTrace($"Export AssignmentId '{assignmentId}', csv generated, timer: {sw.ElapsedMilliseconds} ms.");

            var result = new Result
            {
                ExportData = exportData,
                FileName = $"{assignmentName}-{DateTimeOffset.UtcNow:yyyy-MM-dd}.csv"
            };

            return result;
        }

        private async Task<string> GetAssignmentNameOrThrow(Guid searchFirmId, Guid assignmentId, CancellationToken cancellationToken)
        {
            var assignmentFeedIterator = m_DataQuery
               .GetFeedIterator<Assignment, string>(
                                                    searchFirmId.ToString(),
                                                    i => i
                                                        .Where(a => a.Id == assignmentId)
                                                        .Select(a => a.Name),
                                                    2);

            var queryResult = await assignmentFeedIterator.FetchPage(cancellationToken);

            if (!queryResult.Any())
                throw new ResourceNotFoundException(nameof(Assignment), assignmentId.ToString(), nameof(assignmentId));

            if (queryResult.Count != 1)
                throw new ParamValidationFailureException(nameof(Assignment), $"{assignmentId} returned multiple records");

            return string.IsNullOrWhiteSpace(queryResult.Single()) ? "Assignment" : queryResult.Single();
        }
    }
}
