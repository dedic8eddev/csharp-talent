using Ikiru.Parsnips.Api.Controllers.Recaptcha;
using Ikiru.Parsnips.Api.Recaptcha;
using Ikiru.Parsnips.Api.Recaptcha.Models;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ApiException;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Recaptcha
{
    public class PostTests
    {
        private Post.Command m_Command;
        private Mock<IRecaptchaApi> m_RecaptchaApiMock;
        private RecaptchaVerifyResponseModel m_RecaptchaVerifyResponseModel;

        public PostTests()
        {
            m_RecaptchaApiMock = new Mock<IRecaptchaApi>();

            m_Command = new Post.Command()
                        {
                            Token = "zxcvbnmasdfghjkertyu456789retyuifghbvnmfgh6y7u8i9tgfyhjuk"
                        };
        }

        [Fact]
        public async Task PostValidateRecaptchaTokenSuccess()
        {
            // Given
            var controller = CreateController();
            m_RecaptchaVerifyResponseModel = new RecaptchaVerifyResponseModel()
                                             {
                                                 ErrorCodes = null,
                                                 Hostname = "Testhostname",
                                                 Success = true,
                                                 ChallengeTimestamp = DateTimeOffset.Now.AddMinutes(-1)
                                             };

            m_RecaptchaApiMock.Setup(x => x.VerifyToken(It.IsAny<RecaptchaVerifyRequesteModel>()))
                              .Returns<RecaptchaVerifyRequesteModel>(a => Task.FromResult(m_RecaptchaVerifyResponseModel));

            // When
            var actionResult = await controller.Post(m_Command);

            // Then
            var result = (Post.Result)((OkObjectResult)actionResult).Value;
            Assert.Equal(m_RecaptchaVerifyResponseModel.Success, result.Success);
            Assert.NotNull(result.ChallengeTimestamp);
            Assert.Equal(m_RecaptchaVerifyResponseModel.ChallengeTimestamp, result.ChallengeTimestamp);
            Assert.NotEqual(DateTimeOffset.MinValue, result.ChallengeTimestamp);
            Assert.NotEqual(DateTimeOffset.MaxValue, result.ChallengeTimestamp);
            Assert.Equal(m_RecaptchaVerifyResponseModel.Hostname, result.HostName);
            Assert.Equal(m_RecaptchaVerifyResponseModel.ErrorCodes, result.ErrorCodes);

            m_RecaptchaApiMock.Verify(x => x.VerifyToken(It.Is<RecaptchaVerifyRequesteModel>(r => r.Response == m_Command.Token)), Times.Once);

        }

        [Fact]
        public async Task PostValidateRecaptchaTokenApiFailureException()
        {
            // Given
            m_RecaptchaApiMock.Setup(x => x.VerifyToken(It.IsAny<RecaptchaVerifyRequesteModel>()))
                              .Returns(() =>  throw new ExternalApiException("", ""));

            var controller = CreateController();
            m_RecaptchaVerifyResponseModel = new RecaptchaVerifyResponseModel()
                                             {
                                                 ErrorCodes = new []{"missing-input-secret"},
                                                 Hostname = "Testhostname",
                                                 Success = false,
                                                 ChallengeTimestamp = DateTimeOffset.Now.AddMinutes(-1)
                                             };

            // When
            var ex = await Record.ExceptionAsync(() => controller.Post(m_Command));

            // Then
            Assert.NotNull(ex);
            Assert.IsType<ExternalApiException>(ex);

            m_RecaptchaApiMock.Verify(x => x.VerifyToken(It.Is<RecaptchaVerifyRequesteModel>(r => r.Response == m_Command.Token)), Times.Once);

        }


        private RecaptchaController CreateController()
        {
            return new ControllerBuilder<RecaptchaController>()
                  .AddTransient(m_RecaptchaApiMock.Object)
                  .Build();
        }
    }
}
