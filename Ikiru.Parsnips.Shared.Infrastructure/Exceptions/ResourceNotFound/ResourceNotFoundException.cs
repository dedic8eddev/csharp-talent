using System;

namespace Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ResourceNotFound
{
    public class ResourceNotFoundException : Exception
    {
        /// <summary>
        /// Throws a Not Found exception, for the given Resource Name and parameter value for the given parameter Key Name.
        /// </summary>
        public ResourceNotFoundException(string resourceName, string resourceId, string keyName = "Id")
        {
            ResourceName = resourceName;
            ResourceId = resourceId;

            if (!string.IsNullOrWhiteSpace(keyName)) 
                KeyName = keyName;
        }

        /// <summary>
        /// Throws a Not Found exception *without* parameter name or value for which the resource was not found.
        /// </summary>
        public ResourceNotFoundException(string resourceName)
        {
            ResourceName = resourceName;
        }

        public string ResourceName { get; set; }

        public string ResourceId { get; set; }

        public string KeyName { get; set; }

        public override string Message => string.IsNullOrEmpty(ResourceId)
                                              ? $"Unable to find '{ResourceName}'"
                                              : $"Unable to find '{ResourceName}' with {KeyName} '{ResourceId}'";
    }
}
