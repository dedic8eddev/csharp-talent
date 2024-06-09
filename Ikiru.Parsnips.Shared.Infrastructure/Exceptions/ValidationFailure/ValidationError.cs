using System.Collections.Generic;

namespace Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure
{
    public class ValidationError
    {
        public string Param { get; }

        public List<object> Errors { get; }

        public ValidationError(string param, params object[] errors)
        {
            Param = param;
            Errors = new List<object>(errors);
        }
    }
}