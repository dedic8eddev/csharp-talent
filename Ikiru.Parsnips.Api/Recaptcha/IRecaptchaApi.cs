using System.Threading.Tasks;
using Ikiru.Parsnips.Api.Recaptcha.Models;
using Refit;

namespace Ikiru.Parsnips.Api.Recaptcha
{
    public interface IRecaptchaApi
    {
        // https://www.google.com/recaptcha/api/
        [Post("/siteverify")]
        Task<RecaptchaVerifyResponseModel> VerifyToken([Body(BodySerializationMethod.UrlEncoded)]RecaptchaVerifyRequesteModel request);
    }
}
