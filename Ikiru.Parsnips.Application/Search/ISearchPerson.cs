using System.Threading.Tasks;

namespace Ikiru.Parsnips.Application.Search
{
    public interface ISearchPerson
    {
        Task<Model.SearchResult> SearchByName(Model.SearchQuery queryModel);
    }
}