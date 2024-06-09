using FluentValidation;
using Ikiru.Parsnips.Api.Controllers.Sectors;
using Ikiru.Parsnips.Api.Services;
using Ikiru.Parsnips.DomainInfrastructure;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Persons.ProfessionalExperience
{
    public class Put
    {
        public class Command : IRequest<Result>
        {
            internal Guid PersonId { get; set; }

            public List<RequestSector> Sectors { get; set; }
            public List<string> Keywords { get; set; }

            public class RequestSector
            {
                public string SectorId { get; set; }
            }
        }
                
        public class Result
        {
            public List<ResultSector> Sectors { get; set; }
            public List<string> Keywords { get; set; }

            public class ResultSector : Command.RequestSector
            {
                public Sector LinkSector { get; set; }
            }

            public class Sector
            {
                public string SectorId { get; set; }
                public string Name { get; set; }
            }
        }

        public class Handler : IRequestHandler<Command, Result>
        {
            private readonly PersonFetcher m_PersonFetcher;
            private readonly DataStore m_DataStore;

            public Handler(PersonFetcher personFetcher, DataStore dataStore)
            {
                m_PersonFetcher = personFetcher;
                m_DataStore = dataStore;
            }

            public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
            {
                var person = await m_PersonFetcher.FetchPersonOrThrow(command.PersonId, cancellationToken);

                var sectors = command.Sectors.ToDictionary(s => s.SectorId, s => SectorsLookup.GetSectorById(s.SectorId)?.Name);
                var keywords = command.Keywords.ToList();

                var missingSectors = sectors.Where(kvp => kvp.Value is null).Select(kvp => $"'{kvp.Key}'").ToList();
                if (missingSectors.Any())
                    throw new ParamValidationFailureException(nameof(Command.Sectors), $"Invalid Sectors: {string.Join(",", missingSectors)}");
                
                person.SectorsIds = sectors.Keys.ToList();
                person.Keywords = keywords;

                person = await m_DataStore.Update(person, cancellationToken);

                return new Result
                       {
                           Sectors = person.SectorsIds.Select(s => new Result.ResultSector
                                                                   {
                                                                       SectorId = s,
                                                                       LinkSector = new Result.Sector
                                                                       {
                                                                           SectorId = s,
                                                                           Name = sectors[s]
                                                                       }
                                                                   }).ToList(),
                           Keywords = person.Keywords
                       };
            }
        }
    }
}