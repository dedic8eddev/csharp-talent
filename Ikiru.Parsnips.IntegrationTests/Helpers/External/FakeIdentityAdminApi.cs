using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi;
using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi.Models;
using Moq;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.IntegrationTests.Helpers.External
{
    /// <summary>
    /// Fake Identity Admin API.  There are a few scenarios where Unit Tests aren't catching issues, so add Integration Test
    /// simulator endpoints which require happy path.
    /// </summary>
    public class FakeIdentityAdminApi
    {
        private readonly Mock<IIdentityAdminApi> m_Mock;

        public IIdentityAdminApi Instance => m_Mock.Object;

        public FakeIdentityAdminApi()
        {
            var createdIdentityUserId = Guid.NewGuid();

            m_Mock = new Mock<IIdentityAdminApi>();

            // Mock Create User to return specific User ID
            m_Mock.Setup(i => i.CreateUser(It.IsAny<CreateUserRequest>()))
                  .ReturnsAsync(() => new CreateUserResult { Id = createdIdentityUserId, UserName = "newUserName" });

            // Mock Update to Fail for all values
            m_Mock.Setup(i => i.UpdateUser(It.IsAny<Guid>(), It.IsAny<UpdateUserRequest>()))
                  .Throws(new InvalidOperationException("Identity Server API called with Unknown User"));

            // Mock Update to Succeed for User that was Created
            m_Mock.Setup(i => i.UpdateUser(It.Is<Guid>(g => g == createdIdentityUserId), It.IsAny<UpdateUserRequest>()))
                  .Returns(Task.CompletedTask);

            m_Mock.Setup(i => i.GetUser(It.IsAny<string>()))
                  .ReturnsAsync(() => new Refit.ApiResponse<User>(new HttpResponseMessage(HttpStatusCode.NotFound), new User()));
        }
    }
}
