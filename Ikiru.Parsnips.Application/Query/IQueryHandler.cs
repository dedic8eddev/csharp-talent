using System.Threading.Tasks;

namespace Ikiru.Parsnips.Application.Query
{
    public interface IQueryHandler<TIn, TOut>
    {
        Task<TOut> Handle(TIn query);
    }
}
