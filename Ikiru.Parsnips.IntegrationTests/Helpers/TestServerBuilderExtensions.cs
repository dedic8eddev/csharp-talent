using Microsoft.Extensions.DependencyInjection;

namespace Ikiru.Parsnips.IntegrationTests.Helpers
{
    public static class TestServerBuilderExtensions
    {
        public static TestServerBuilder AddSingleton<TService>(this TestServerBuilder testServerBuilder, TService implementation) where TService : class
        {
            testServerBuilder.ServiceCollection.AddSingleton(_ => implementation);
            return testServerBuilder;
        }
        
        public static TestServerBuilder AddTransient<T, TService>(this TestServerBuilder testServerBuilder) where TService : class, T
                                                                                                            where T : class
        {
            testServerBuilder.ServiceCollection.AddTransient<T, TService>();
            return testServerBuilder;
        }
    }
}