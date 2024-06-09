using AutoMapper;
using Ikiru.Parsnips.Api.Services;
using Ikiru.Parsnips.Application.Services.DataPool;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.DomainInfrastructure;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Controllers.Persons
{
    public class GetSimilarList
    {
        private const int _PAGE_SIZE = 20;

        public class Query : IRequest<Result>
        {
            public Guid PersonId { get; set; }
            public int PageSize { get; set; }
            public bool ExactSearch { get; set; }
        }

        public class Result
        {
            public Person[] SimilarPersons { get; set; }

            public class Person
            {
                public Guid? Id { get; set; }
                public Guid DataPoolPersonId { get; set; }
                public string Name { get; set; }
                public string JobTitle { get; set; }
                public string Company { get; set; }
                public List<PersonWebsite> WebSites { get; set; }
                public Address Location { get; set; }

                public class Address
                {
                    public string CountryName { get; set; } = "";
                    public string CityName { get; set; } = "";
                    public string AddressLine { get; set; } = "";
                }
            }
        }

        public class Handler : IRequestHandler<Query, Result>
        {
            private readonly PersonFetcher m_PersonFetcher;
            private readonly IMapper m_Mapper;
            private readonly IDataPoolService m_DataPoolService;
            private readonly DataQuery m_DataQuery;

            public Handler(PersonFetcher personFetcher, IMapper mapper,
                           IDataPoolService dataPoolService, DataQuery dataQuery)
            {
                m_PersonFetcher = personFetcher;
                m_Mapper = mapper;
                m_DataPoolService = dataPoolService;
                m_DataQuery = dataQuery;
            }

            public async Task<Result> Handle(Query query, CancellationToken cancellationToken)
            {
                if (query.PageSize <= 0)
                    query.PageSize = _PAGE_SIZE;

                var person = await m_PersonFetcher.FetchPersonOrThrow(query.PersonId, cancellationToken);

                IEnumerable<Shared.Infrastructure.DataPoolApi.Models.Person.Person> similarPersons;

                if (person.DataPoolPersonId != null)
                {
                    similarPersons = await m_DataPoolService.GetSimilarPersons(person.DataPoolPersonId.Value, query.PageSize, query.ExactSearch, cancellationToken);
                }
                else
                {
                    var searchString = CreateSearchString(person, query.ExactSearch);
                    similarPersons = await m_DataPoolService.GetSimilarPersons(searchString, query.PageSize, cancellationToken);
                }

                var resultPersons = m_Mapper.Map<Result.Person[]>(similarPersons);

                Parallel.ForEach(resultPersons, p => p.WebSites.Sort());

                await SetPersonId(person.SearchFirmId, resultPersons, cancellationToken);

                var result = new Result
                {
                    SimilarPersons = resultPersons
                };

                return result;
            }

            public class PersonIdMatch
            {
                public Guid PersonId { get; set; }
                public Guid DataPoolPersonId { get; set; }
            }

            private async Task SetPersonId(Guid searchFirmId, Result.Person[] similarPersons, CancellationToken cancellationToken)
            {
                if (!similarPersons.Any())
                    return;

                var ids = similarPersons.Select(p => p.DataPoolPersonId).ToList();

                if (!ids.Any())
                    return;

                var persons =
                    await m_DataQuery.FetchAllItems(searchFirmId.ToString(),
                                                                   (IOrderedQueryable<Domain.Person> q) => q
                                                                   .Where(p => p.DataPoolPersonId != null && ids.Contains(p.DataPoolPersonId.Value))
                                                                   .Select(p => new PersonIdMatch { PersonId = p.Id, DataPoolPersonId = p.DataPoolPersonId.Value }),
                                                                   cancellationToken);

                foreach (var personIdMatch in persons)
                {
                    var person = similarPersons.SingleOrDefault(p => p.DataPoolPersonId == personIdMatch.DataPoolPersonId);

                    if (person == null)
                        continue;

                    person.Id = personIdMatch.PersonId;
                }
            }

            private static string CreateSearchString(Domain.Person person, bool exactSearch)
            {
                var queryBuilder = new StringBuilder();

                AppendString(queryBuilder, person.JobTitle, exactSearch);
                AppendString(queryBuilder, person.Organisation, true);

                return queryBuilder.ToString();
            }

            private static void AppendString(StringBuilder builder, string value, bool exactSearch)
            {
                if (string.IsNullOrWhiteSpace(value)) return;

                if (builder.Length > 0)
                    builder.Append(" ");

                if (value.Contains('"'))
                    value = value.Replace("\"", "\\\"");

                if (exactSearch) builder.Append("\"");
                builder.Append(value);
                if (exactSearch) builder.Append("\"");
            }
        }
    }
}
