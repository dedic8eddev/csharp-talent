using System;
using System.Collections.Generic;
using System.Text;

namespace Ikiru.Parsnips.Application.Command
{
    public class CommandResponse<TResponse>
    {
        private List<Response> _validationErrors;
        
        public TResponse ResponseModel {get; set; }

        public List<Response> ValidationErrors
        {
            get
            {
                if (_validationErrors == null)
                    _validationErrors = new List<Response>();

                return _validationErrors;
            }
            set { _validationErrors = value; }
        }
    }
}
