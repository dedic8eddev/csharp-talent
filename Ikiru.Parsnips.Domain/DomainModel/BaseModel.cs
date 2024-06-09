using AutoMapper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json.Serialization;

namespace Ikiru.Parsnips.Domain.DomainModel
{
    public abstract class BaseModel
    {
        protected BaseModel()
        {
        }

        [Newtonsoft.Json.JsonIgnore]
        [JsonIgnore]
        [IgnoreMap]
        public List<ValidationResult> ValidationResults { get; private set; }

        public List<ValidationResult> Validate(object obj = null)
        {
            if (obj == null)
            {
                obj = this;
            }

            if (ValidationResults == null)
            {
                ValidationResults = new List<ValidationResult>();
            }

            var context = new System.ComponentModel.DataAnnotations.ValidationContext(obj, serviceProvider: null, items: null);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(obj, context, results, true);

            if (!isValid)
                foreach (var validationResult in results)
                    ValidationResults.Add(validationResult);
            foreach (var prop in obj.GetType().GetProperties())
            {
                if (prop.PropertyType == typeof(string) || prop.PropertyType.IsValueType) continue;
                var value = prop.GetValue(obj);
                if (value == null) continue;
                var isEnumerable = value as IEnumerable;
                if (isEnumerable == null)
                    Validate(value);
                else
                    foreach (var nestedModel in isEnumerable)
                        Validate(nestedModel);
            }
            return ValidationResults;
        }

    }
}
