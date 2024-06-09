using System;
using System.Net;
using System.Threading.Tasks;
using Ikiru.Parsnips.IntegrationTests.Helpers;
using Ikiru.Parsnips.IntegrationTests.Helpers.External;
using Ikiru.Parsnips.IntegrationTests.Helpers.HttpMessages;
using Xunit;
using Xunit.Abstractions;

namespace Ikiru.Parsnips.IntegrationTests.Features.Api.Recaptcha
{
    [Collection(nameof(IntegrationTestCollection))]
    public class RecaptchaTests : IntegrationTestBase, IClassFixture<RecaptchaTests.RecaptchaTestsClassFixture>
    {
        private readonly RecaptchaTestsClassFixture m_ClassFixture;

        public sealed class RecaptchaTestsClassFixture : IDisposable
        {
            public IntTestServer Server { get; }

            public RecaptchaTestsClassFixture()
            {
                Server = new TestServerBuilder()
                        .AddSingleton(FakeRecaptchaApi.Setup().Object)
                        .Build();
            }

            public void Dispose()
            {
                Server.Dispose();
            }
        }

        public RecaptchaTests(IntegrationTestFixture fixture, RecaptchaTestsClassFixture classFixture, ITestOutputHelper output)
            : base(fixture, output)
        {
            m_ClassFixture = classFixture;
        }

        [Fact]
        public async Task PostRecaptcha()
        {
            // Given
            var command = new 
                          {
                              Token = "zxcvbnmasdfghjkertyu456789retyuifghbvnmfgh6y7u8i9tgfyhjuk"
                          };

            // When
            var response = await m_ClassFixture.Server.UnauthClient.PostAsync("/api/recaptcha", new JsonContent(command));

            // Then
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);
            
            var r = new
                    {
                        Success = true,
                        ChallengeTimestamp = DateTimeOffset.MinValue,
                        HostName = string.Empty,
                        ErrorCodes = Array.Empty<string>(),
                        Score = 0.0,
                        Action = string.Empty
                    };
            var responseJson = await response.Content.DeserializeToAnonymousType(r);
            Assert.NotNull(responseJson);
            
            Assert.True(responseJson.Success);
            Assert.NotEqual(DateTimeOffset.MinValue, responseJson.ChallengeTimestamp);
            Assert.NotEmpty(responseJson.HostName);
            Assert.Null(responseJson.ErrorCodes);
            Assert.IsType<double>(responseJson.Score);
            Assert.NotEmpty(responseJson.Action);
        }
    }
}
