using Ikiru.Persistence.Repository;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ikiru.Parsnips.Application.Persistence
{
    public class Repository<TModel> : RepositoryBase<TModel>
    {
        private IRepository _repository;

        public Repository(IRepository repository) : base(repository)
        {

        }
    }
}
