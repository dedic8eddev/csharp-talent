using Ikiru.Parsnips.Api.Authentication;
using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Persistence.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Security.Claims;

namespace Ikiru.Parsnips.UnitTests.Helpers
{
    public static class ClassBuilderExtensions
    {
        public static ClassBuilder<T> SetFakeCloud<T>(this ClassBuilder<T> builder, FakeCloud fakeCloud) where T : class
        {
            builder.ServiceCollection.AddTransient(_ => fakeCloud.BlobServiceClient.Object);
            return builder;
        }

        public static ClassBuilder<T> SetFakeCloudQueue<T>(this ClassBuilder<T> builder, FakeStorageQueue fakeStorageQueue) where T : class
        {
            builder.ServiceCollection.AddTransient(_ => fakeStorageQueue.QueueServiceClient.Object);
            return builder;
        }

        public static ClassBuilder<T> SetFakeCosmos<T>(this ClassBuilder<T> builder, FakeCosmos fakeCosmos) where T : class
        {
            builder.ServiceCollection.AddTransient(_ => fakeCosmos.FeedIteratorProvider.Object); // Register FeedProvider too
            builder.ServiceCollection.AddTransient(_ => fakeCosmos.MockClient.Object);

            return builder;
        }

        public static ClassBuilder<T> SetFakeRepository<T>(this ClassBuilder<T> builder, FakeRepository fakeRepository) where T : class
        {
            builder.ServiceCollection.AddTransient<IRepository>(_ => fakeRepository);

            return builder;
        }

        public static ClassBuilder<T> AddTransient<T, TService>(this ClassBuilder<T> builder, TService implementation)
            where T : class where TService : class
        {
            builder.ServiceCollection.AddTransient(_ => implementation);
            return builder;
        }

        private static ClassBuilder<T> SetHttpContextUser<T>(this ClassBuilder<T> builder, ClaimsPrincipal principal)
            where T : class
        {
            var context = new DefaultHttpContext
            {
                User = principal
            };

            builder.ServiceCollection.AddTransient(_ => Mock.Of<IHttpContextAccessor>(a => a.HttpContext == context));
            builder.ServiceCollection.AddTransient<AuthenticatedUserAccessor>();
            return builder;
        }

        public static ClassBuilder<T> SetSearchFirmUser<T>(this ClassBuilder<T> builder, Guid? searchFirmId, Guid? userId = null)
            where T : class
        {
            var claims = searchFirmId == null
                             ? new Claim[0]
                             : new[]
                                 {
                                     new Claim("SearchFirmId", searchFirmId.ToString()),
                                     new Claim("UserId", userId?.ToString() ?? Guid.Empty.ToString()),
                                 };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));
            return builder.SetHttpContextUser(principal);
        }
    }
}
