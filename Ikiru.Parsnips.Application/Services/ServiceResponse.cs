using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Ikiru.Parsnips.Application.Services
{
    public class ServiceResponse<TResponse>
    {
        private List<ValidationResult> _validationErrors;

        public TResponse Value { get; set; }

        public List<ValidationResult> ValidationErrors
        {
            get
            {
                if (_validationErrors == null)
                    _validationErrors = new List<ValidationResult>();

                return _validationErrors;
            }
            set { _validationErrors = value; }
        }

        public void AddCustomValidationError(string message, string property)
        {
            ValidationErrors.Add(new ValidationResult(message, new List<string>() { property }));            
        }


    }
}
