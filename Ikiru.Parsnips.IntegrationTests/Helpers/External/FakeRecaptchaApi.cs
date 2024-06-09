using System;
using System.Threading.Tasks;
using Ikiru.Parsnips.Api.Recaptcha;
using Ikiru.Parsnips.Api.Recaptcha.Models;
using Moq;

namespace Ikiru.Parsnips.IntegrationTests.Helpers.External
{
    public static class FakeRecaptchaApi
    {
        public static Mock<IRecaptchaApi> Setup()
        {
            var recaptchaMock = new Mock<IRecaptchaApi>();

            recaptchaMock.Setup(x => x.VerifyToken(It.IsAny<RecaptchaVerifyRequesteModel>()))
                         .Returns(Task.FromResult(new RecaptchaVerifyResponseModel()
                                                  {
                                                      ErrorCodes = null,
                                                      Hostname = "localhost",
                                                      ChallengeTimestamp = DateTimeOffset.Now,
                                                      Action = "integration test",
                                                      Score = 0.4,
                                                      Success = true
                                                  }));

            return recaptchaMock;
        }

    }
}
