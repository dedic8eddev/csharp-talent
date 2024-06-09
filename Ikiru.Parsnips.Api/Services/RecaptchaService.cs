using Ikiru.Parsnips.Api.Recaptcha;
using Ikiru.Parsnips.Api.Recaptcha.Models;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ApiException;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Services
{
    public class RecaptchaService
    {
        private readonly IRecaptchaApi m_RecaptchaApi;
        private readonly RecaptchaSettings m_RecaptchaSettings;
        private readonly ILogger<RecaptchaService> m_Logger;

        public RecaptchaService(IRecaptchaApi recaptchaApi,
                                IOptions<RecaptchaSettings> recaptchaSettings,
                                ILogger<RecaptchaService> logger)
        {
            m_RecaptchaApi = recaptchaApi;
            m_RecaptchaSettings = recaptchaSettings.Value;
            m_Logger = logger;
        }

        public async Task<RecaptchaVerifyResponseModel> Verify(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                m_Logger.LogError($"Recaptcha: Unable to verify token of value : {token}");
                throw new ParamValidationFailureException(token, "recaptcha token missing.");
            }

            var request = new RecaptchaVerifyRequesteModel
            {
                Secret = m_RecaptchaSettings.Secret,
                Response = token
            };

            try
            {
                var response = await m_RecaptchaApi.VerifyToken(request);
                
                ProcessErrorResponse(response.ErrorCodes);
                return response;
               
            }
            catch (ExternalApiException ex)
            {
                throw new ExternalApiException("Recaptcha", "Unable to verify recaptcha token", ex);
            }
            
        }


        private void ProcessErrorResponse(IEnumerable<string> errorCodes)
        {
            if (errorCodes == null)
                return;

            foreach (var error in errorCodes)
            {
                switch (error)
                {
                    case "missing-input-secret":
                        m_Logger.LogError($"{error}: The secret parameter is missing.");
                        break;

                    case "invalid-input-secret":
                        m_Logger.LogError($"{error}: The secret parameter is invalid or malformed.");
                        break;

                    case "missing-input-response":
                        m_Logger.LogError($"{error}: The response parameter is missing.");
                        break;

                    case "invalid-input-response":
                        m_Logger.LogError($"{error}:The response parameter is invalid or malformed.");
                        break;

                    case "bad-request":
                        m_Logger.LogError($"{error}: The request is invalid or malformed.");
                        break;

                    case "timeout-or-duplicate":
                        m_Logger.LogError($"{error}:The response is no longer valid: either is too old or has been used previously.");
                        break;
                    default:
                        m_Logger.LogError($"{error}: Unkown response error code.");
                        break;
                }
            }

            throw new ExternalApiException("Recaptcha", "Unable to verify recaptcha token");
        }
    }
}
