using System;

namespace Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ApiException
{
    public class ExternalApiException: Exception
    {
        public string ResourceName { get; }

        public ExternalApiException(string resourceName, string message) : base(message)
        {
            ResourceName = resourceName;
        }

        public ExternalApiException(string resourceName, string message, Exception innerException) : base(message, innerException)
        {
            ResourceName = resourceName;
        }
    }
}
