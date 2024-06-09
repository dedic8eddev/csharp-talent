using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace Ikiru.Parsnips.Domain.ValidationAttributes
{
    public class GroupedPropertiesRequiredNotEmpty : ValidationAttribute
    {
        private const string DefaultErrorMessageFormatString = "The {0} field is required.";
        private readonly string[] _dependentProperties;

        /// <summary>
        /// Pass a colection of property names to be null checked together.
        /// </summary>
        /// <param name="dependentProperty"></param>
        public GroupedPropertiesRequiredNotEmpty(string[] dependentProperties)
        {
            _dependentProperties = dependentProperties;
            ErrorMessage = DefaultErrorMessageFormatString;
        }

        protected override ValidationResult IsValid(Object value, ValidationContext validationContext)
        {
            Object instance = validationContext.ObjectInstance;
            Type type = instance.GetType();

            var propertyContainsValue = new List<bool>();

            foreach (var property in _dependentProperties)
            {
                Object propertyValue = type.GetProperty(property).GetValue(instance, null);

                var collection = propertyValue as IList;

                if (collection != null)
                {
                    if (collection.Count > 0)
                    {
                        propertyContainsValue.Add(true);
                    }
                    else
                    {
                        propertyContainsValue.Add(false);
                    }
                }
                else
                {
                    if (propertyValue == null)
                    {
                        propertyContainsValue.Add(false);
                    }
                    else
                    {
                        propertyContainsValue.Add(true);
                    }
                }
              
            }

            if (propertyContainsValue.Exists(pv => pv == false) && propertyContainsValue.Exists(pv => pv == true))
            {
                return new ValidationResult(String.Join(", ", _dependentProperties) + " together are required to have values. ", _dependentProperties);
            }

            return ValidationResult.Success;

        }
    }
}
