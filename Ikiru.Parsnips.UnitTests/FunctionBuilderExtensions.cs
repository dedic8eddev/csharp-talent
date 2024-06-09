using Ikiru.Parsnips.UnitTests.Helpers;
using Ikiru.Persistence.Repository;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace Ikiru.Parsnips.UnitTests.Functions
{
    public static class FunctionBuilderExtensions
    {
        public static FunctionBuilder<T> AddTransient<T, TService>(this FunctionBuilder<T> builder, TService implementation)
             where T: class where TService : class
        {
            builder.ServiceCollection.AddTransient(_ => implementation);
            return builder;
        }

        public static FunctionBuilder<T> SetFakeCosmos<T>(this FunctionBuilder<T> builder, FakeCosmos fakeCosmos) where T : class
        {
            builder.ServiceCollection.AddTransient(_ => fakeCosmos.MockClient.Object);
            builder.ServiceCollection.AddTransient(_ => fakeCosmos.FeedIteratorProvider.Object); // Register FeedProvider too
            return builder;
        }

        public static FunctionBuilder<T> SetFakeRepository<T>(this FunctionBuilder<T> builder, FakeRepository fakeRepository) where T : class
        {
            builder.ServiceCollection.AddTransient<IRepository>(_ => fakeRepository);

            return builder;
        }

        public static FunctionBuilder<T> SetFakeCloud<T>(this FunctionBuilder<T> builder, FakeCloud fakeCloud) where T : class
        {
            builder.ServiceCollection.AddTransient(_ => fakeCloud.BlobServiceClient.Object);
            return builder;
        }
        
        public static FunctionBuilder<T> SetFakeCloudQueue<T>(this FunctionBuilder<T> builder, FakeStorageQueue fakeStorageQueue) where T : class
        {
            builder.ServiceCollection.AddTransient(_ => fakeStorageQueue.QueueServiceClient.Object);
            return builder;
        }

        /// <summary>
        /// Quick fix to make emails work as published email template html location is different to when build.
        /// This should be treated as a technical debt and removed when have time.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static FunctionBuilder<T> CopyEmailFolder<T>(this FunctionBuilder<T> builder) where T : class
        {
            var binPath = System.Reflection.Assembly.GetAssembly(typeof(T)).Location;
            var binDirectory = Path.GetDirectoryName(binPath);
            var parentDirectory = Directory.GetParent(binDirectory).ToString();

            var emailPath = Path.Combine(binDirectory, "Email\\EmailTemplate.html");

            if (File.Exists(emailPath))
            {
                var newEmailPath = Path.Combine(parentDirectory, "Email\\EmailTemplate.html");
                if (!File.Exists(newEmailPath))
                {
                    var newEmailDirectory = Path.Combine(parentDirectory, "Email");
                    if (!Directory.Exists(newEmailDirectory))
                        Directory.CreateDirectory(newEmailDirectory);

                    File.Copy(emailPath, newEmailPath);
                }
            }

            return builder;
        }
    }
}
