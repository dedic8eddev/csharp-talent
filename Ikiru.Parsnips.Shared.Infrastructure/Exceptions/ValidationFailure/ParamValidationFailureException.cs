using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure
{
    public class ParamValidationFailureException : Exception
    {
        public ParamValidationFailureException(string param, string message) : base(message)
        {
            ValidationErrors = new List<ValidationError>
                               {
                                   new ValidationError(param, message)
                               };
        }
        
        public ParamValidationFailureException(List<ValidationError> errors) : base("There were problems with the provided data")
        {
            ValidationErrors = errors;
        }

        public ParamValidationFailureException(List<ValidationResult> validationResults)
        {
            ValidationErrors = validationResults.Select(r => new ValidationError(string.Join(',', r.MemberNames), r.ErrorMessage)).ToList();
        }

        public List<ValidationError> ValidationErrors { get; }
        
        public Dictionary<string, object[]> ToDictionary()
        {
            return ValidationErrors.GroupBy(e => e.Param, 
                                            e => ValidationErrors.Where(v => v.Param == e.Param).SelectMany(v => v.Errors),
                                            (key, g) => new KeyValuePair<string, object[]>(key, g.SelectMany(e => e.Select(er => er is string ? (er as string).Replace("{Param}", key) : er)).ToArray())
                                           )
                                   .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }
}
