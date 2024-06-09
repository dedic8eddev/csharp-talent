using Ikiru.Parsnips.Domain.Base;
using Ikiru.Persistence.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Ikiru.Parsnips.UnitTests.Helpers
{
    public class FakeRepository : IRepository
    {
        private readonly HashSet<string> _breakDeleteFlowForId = new HashSet<string>();
        private readonly Dictionary<string, Dictionary<string, object>> _repository = new Dictionary<string, Dictionary<string, object>>();

        public void AddToRepository<T>(Guid id, T item) => AddToRepository(id.ToString(), item);

        public void AddToRepository(params object[] items)
        {
            if (items == null)
                return;

            foreach (var item in items)
            {
                var id = GetId(item);
                AddToRepository(id, item);
            }
        }

        public void AddToRepository<T>(string id, T item)
        {
            var name = GetName(item);

            if (!_repository.ContainsKey(name))
                _repository[name] = new Dictionary<string, object>();

            _repository[name][id] = item;
        }

        private string GetName<T>() => typeof(T).Name;
        private string GetName(object item) => item.GetType().Name;

        private T Repo<T>(string id)
        {
            var container = _repository[GetName<T>()];

            if (container.ContainsKey(id))
                return Clone((T)container[id]);

            return default;
        }

        private IEnumerable<T> Repo<T>()
        {
            if (!_repository.ContainsKey(GetName<T>()))
                return new List<T>();

            var value = _repository[GetName<T>()].Select(pair => Clone((T)pair.Value));
            return value;
        }

        /// <summary>
        /// breaks the link between in-memory objects, so changing objects without saving will break unit tests
        /// </summary>
        private T Clone<T>(T item)
        {
            var serialized = JsonConvert.SerializeObject(item);
            var copy = JsonConvert.DeserializeObject<T>(serialized);
            return copy;
        }

        private string GetId(object item) => (item as DomainObject)?.Id.ToString() ?? item.GetHashCode().ToString(); //Todo: replace hashcode with something more robust as it could be different if object changes

        private string GetPartitionKey<T>(T item)
        {
            var instance = typeof(T);

            if (instance.IsSubclassOf(typeof(MultiTenantedDomainObject)))
                return ((MultiTenantedDomainObject)(object)item).SearchFirmId.ToString();

            if (instance.IsSubclassOf(typeof(IDiscriminatedDomainObject)))
                return ((IDiscriminatedDomainObject)item).Discriminator;

            return null;
        }

        private IEnumerable<T> FilterByPartitionKey<T>(string partitionKey, IEnumerable<T> data)
        {
            if (string.IsNullOrEmpty(partitionKey))
                return data;

            var instance = typeof(T);

            if (instance.IsSubclassOf(typeof(MultiTenantedDomainObject)))
                data = data.Where(i => { var t = i.GetType(); return t.GetProperty("SearchFirmId").GetValue(i, null).ToString() == partitionKey; });
            else if (instance.IsSubclassOf(typeof(IDiscriminatedDomainObject)))
                data = data.Where(i => { var t = i.GetType(); return (string)t.GetProperty("Discriminator").GetValue(i, null) == partitionKey; });

            return data;
        }

        #region IRepository

        public Task<T> GetItem<T>(string partitionKey, string itemId)
        {
            var item = Repo<T>(itemId);

            var searchFirmId = (item as MultiTenantedDomainObject)?.SearchFirmId.ToString(); //only limited support of partition key, implement in full when needed
            if (searchFirmId != partitionKey)
                item = default;

            return Task.FromResult(item);
        }

        public Task<List<T>> GetByQuery<T>(Expression<Func<T, bool>> expression) => Task.FromResult(Repo<T>().Where(expression.Compile()).ToList());

        public Task<List<TOut>> GetByQuery<TIn, TOut>(string partitionKey, Expression<Func<IOrderedQueryable<TIn>, IQueryable<TOut>>> filter, int? totalItemCount = null)
        {
            var filterFunc = filter.Compile();

            var filteredBySearchFirm = FilterByPartitionKey(partitionKey, Repo<TIn>());

            var orderedQueryable = filteredBySearchFirm.AsMockedOrderedQueryable();

            var result = filterFunc(orderedQueryable);
            if (totalItemCount != null)
                result = result.Take(totalItemCount.Value);

            return Task.FromResult(result.ToList());
        }

        public Task<int> Count<T>(Expression<Func<T, bool>> expression)
        {
            var result = Repo<T>().Where(expression.Compile());
            return Task.FromResult(result.Count());
        }

        public Task<int> Count<T>(string partitionKey, Expression<Func<T, bool>> expression)
        {
            var result = Repo<T>().Where(expression.Compile());

            result = FilterByPartitionKey(partitionKey, result);

            return Task.FromResult(result.Count());
        }

        public Task<T> Add<T>(T item)
        {
            AddToRepository(item);
            return Task.FromResult(Clone(item));
        }

        public Task<T> UpdateItem<T>(T item)
        {
            var id = GetId(item);
            Delete<T>(id);
            AddToRepository(item);

            return Task.FromResult(Clone(item));
        }

        public Task<bool> Delete<T>(T item) => Delete<T>(GetId(item));

        public Task<bool> Delete<T>(string id)
        {
            var repo = _repository[GetName<T>()];
            var result = !_breakDeleteFlowForId.Contains(id) && repo.Remove(id);
            return Task.FromResult(result);
        }

        #endregion

        public void DoNotDeleAndReturnFalseForId(Guid id) => DoNotDeleAndReturnFalseForId(id.ToString());
        public void DoNotDeleAndReturnFalseForId(string id) => _breakDeleteFlowForId.Add(id);

        public Task<bool> Delete<T>(string partitionKey, string id)
        {
            throw new NotImplementedException();
        }

        public Task<List<TOut>> GetBySql<TIn, TOut>(string query)
        {
            Console.WriteLine("Will implement later if takes less than a day");
            return Task.FromResult(new List<TOut>());
        }
    }
}
