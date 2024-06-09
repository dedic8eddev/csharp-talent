using System.Threading.Tasks;

namespace Ikiru.Parsnips.Application.Command
{
    public interface ICommandHandler<TIn, TOut>
    {
        Task<TOut> Handle(TIn command);
    }
}
