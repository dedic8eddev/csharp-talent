using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Ikiru.Parsnips.Api.ModelBinding
{
    /// <summary>
    /// Model Binder used to deserialise comma delimited strings to ExpandList instances.  String values
    /// not mapping to Enum values will be added as Model errors (And so will fail at serialisation layer
    /// like they did with standard model binder)
    /// </summary>
    /// <typeparam name="T">The Enum Type for the ExpandList.</typeparam>
    public class ExpandListBinder<T> : IModelBinder where T : struct, Enum
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
                throw new ArgumentNullException(nameof(bindingContext));

            var modelName = bindingContext.ModelName;

            // Pull the value attempting to be assigned to the Property
            var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);

            if (valueProviderResult == ValueProviderResult.None)
                return Task.CompletedTask;

            bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);

            var rawValue = valueProviderResult.FirstValue;

            // Do nothing if the value is empty
            if (string.IsNullOrEmpty(rawValue))
                return Task.CompletedTask;

            var values = rawValue.Split(',')
                                 .Select(v => v.Trim())
                                 .ToArray();

            var validResult = new ExpandList<T>(values.Length);
            var invalidValues = new List<string>(values.Length);

            var isValid = true;

            var enumNamesToCompare = Enum.GetNames(typeof(T));

            foreach (var value in values)
            {
                if (enumNamesToCompare.Any(x => string.Equals(x, value, StringComparison.CurrentCultureIgnoreCase)))
                {
                    if (isValid) // Skip the parse if we know we are going to return failure
                        validResult.Add(Enum.Parse<T>(value, true));
                }
                else
                {
                    isValid = false;
                    invalidValues.Add(value);
                }
            }

            if (isValid)
                bindingContext.Result = ModelBindingResult.Success(validResult);
            else 
                bindingContext.ModelState.TryAddModelError(modelName, $"Invalid values for {modelName}: {string.Join(',', invalidValues)}");

            return Task.CompletedTask;
        }
    }
}