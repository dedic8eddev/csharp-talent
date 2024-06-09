using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ikiru.Parsnips.Functions.Startup;
using System;

namespace Ikiru.Parsnips.UnitTests.Functions
{
    public class FunctionBuilder<T> where T: class
    {
        private const string _APPSETTINGS_FILE = "appsettings.unittests.functions.json";

        private bool m_Built;
        private ServiceProvider m_ServiceProvider;

        public FunctionBuilder()
        {
            ServiceCollection = new ServiceCollection()
                .AddTransient<T>();
            Startup.ConfigureTestableServices(ServiceCollection, new ConfigurationBuilder().AddJsonFile(_APPSETTINGS_FILE).Build());
        
        }
        
        public T Build()
        {
            if (m_Built)
                throw new InvalidOperationException($"You should only be calling {nameof(Build)}() once!");
            m_Built = true;

            m_ServiceProvider = ServiceCollection.BuildServiceProvider();
            return m_ServiceProvider.GetService<T>();
        }

        internal IServiceCollection ServiceCollection { get; }
        internal TOut GetService<TOut>() => m_ServiceProvider.GetRequiredService<TOut>();
    }
}
