using System;
using System.Text.Json;
using Ikiru.Parsnips.Functions.Parsing.Api.Models;
using Refit;

namespace Ikiru.Parsnips.Functions.Parsing
{
    public class SovrenApiException : Exception
    {
        public SovrenApiException(ApiException innerException) : base(GetRealErrorMessage(innerException), innerException)
        {
        }

        private static string GetRealErrorMessage(ApiException apiException)
        {
            try
            {
                var errorResponse = JsonSerializer.Deserialize<SovrenResponse>(apiException.Content);
                return $"Sovren: {errorResponse.Info?.Code}: {errorResponse.Info?.Message}";
            }
            catch
            {
                return $"[Non-Sovren API error] {apiException.Message} [{apiException.Content}]";
            }
        }
    }
}