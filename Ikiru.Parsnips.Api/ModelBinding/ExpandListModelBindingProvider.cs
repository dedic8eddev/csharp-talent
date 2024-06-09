using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Reflection;

namespace Ikiru.Parsnips.Api.ModelBinding
{
    /// <summary>
    /// Provider to create instance of ExpandListBinder with the correct T type for the incoming property.
    /// </summary>
    public class ExpandListModelBindingProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            
            if (context.Metadata.ModelType.GetTypeInfo().IsGenericType && 
                context.Metadata.ModelType.GetGenericTypeDefinition() == typeof(ExpandList<>))
            {
                var types = context.Metadata.ModelType.GetGenericArguments();
                var binderType = typeof(ExpandListBinder<>).MakeGenericType(types);

                // We could create Dictionary to cache ModelType -> binderInstance in future to avoid the reflection but seems pretty fast
                var binderInstance = (IModelBinder)Activator.CreateInstance(binderType);
                return binderInstance;
            }

            return null;
        }
    }
}
