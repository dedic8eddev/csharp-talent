using Ikiru.Parsnips.UnitTests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Security.Claims;

namespace Ikiru.Parsnips.UnitTests
{
    public static class ControllerBuilderExtensions
    {
        public static ControllerBuilder<T> SetFakeCloud<T>(this ControllerBuilder<T> builder, FakeCloud fakeCloud) where T : ControllerBase
        {
            builder.ServiceCollection.AddTransient(_ => fakeCloud.BlobServiceClient.Object);
            return builder;
        }

        public static ControllerBuilder<T> SetFakeCloudQueue<T>(this ControllerBuilder<T> builder, FakeStorageQueue fakeStorageQueue) where T : ControllerBase
        {
            builder.ServiceCollection.AddTransient(_ => fakeStorageQueue.QueueServiceClient.Object);
            return builder;
        }

        public static ControllerBuilder<T> SetFakeCosmos<T>(this ControllerBuilder<T> builder, FakeCosmos fakeCosmos) where T : ControllerBase
        {
            builder.ServiceCollection.AddTransient(_ => fakeCosmos.FeedIteratorProvider.Object);
            builder.ServiceCollection.AddTransient(_ => fakeCosmos.MockClient.Object);

            return builder;
        }

        public static ControllerBuilder<T> AddTransient<T, TService>(this ControllerBuilder<T> builder, TService implementation)
            where T : ControllerBase where TService : class
        {
            builder.ServiceCollection.AddTransient(_ => implementation);
            return builder;
        }

        public static ControllerBuilder<T> SetHttpContextUser<T>(this ControllerBuilder<T> builder, ClaimsPrincipal principal)
            where T : ControllerBase
        {
            builder.HttpContext.User = principal;
            return builder;
        }
        
        public static ControllerBuilder<T> SetSearchFirmUser<T>(this ControllerBuilder<T> builder, Guid searchFirmId, Guid? userId = null)
            where T : ControllerBase
        {
            var claims = new[]
                         {
                             new Claim("SearchFirmId", searchFirmId.ToString()),
                             new Claim("UserId", userId?.ToString() ?? Guid.Empty.ToString()),
                         };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));
            return builder.SetHttpContextUser(principal);
        }
    }
}
