using AutoMapper;
using Ikiru.Parsnips.Api.Recaptcha.Models;

namespace Ikiru.Parsnips.Api.Controllers.Recaptcha
{
    public class MappingProfile: Profile
    {
        public MappingProfile()
        {
            CreateMap<RecaptchaVerifyResponseModel, Post.Result>()
               .ForMember(dest => dest.ErrorCodes, src => src.UseDestinationValue());
        }
    }
}
