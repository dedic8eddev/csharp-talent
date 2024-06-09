using Ikiru.Persistence.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Application.Persistence
{
    public class RepositoryBase<TIn>
    {
        private readonly IRepository _repository;

        public RepositoryBase(IRepository persistenceService)
        {
            _repository = persistenceService;
        }

        public Task<TIn> Create(TIn model) => _repository.Add(model);
        public Task<TIn> Update(TIn model) => _repository.UpdateItem(model);
        public Task<TIn> GetById(Guid searchFirmId, Guid id) => _repository.GetItem<TIn>(searchFirmId.ToString(), id.ToString());

        [Obsolete("Using this will leak storage structure to business logic")]
        protected async Task<TIn> GetSingleByExpression(Expression<Func<TIn, bool>> expression)
        {
            var result = await _repository.GetByQuery(expression);

            return result.FirstOrDefault();
        }
    }
}
