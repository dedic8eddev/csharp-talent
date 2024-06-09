using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using Ikiru.Persistence;

namespace Ikiru.Parsnips.UnitTests.Helpers
{
    public class ClassBuilder<T> where T : class
    {
        private bool m_Built;

        public IServiceCollection ServiceCollection { get; }

        public ClassBuilder()
        {
            ServiceCollection = new ServiceCollection()
                               .AddLogging()
                               .AddTransient<T>();
        }

        public virtual T Build()
        {
            if (m_Built)
                throw new InvalidOperationException($"You should only be calling {nameof(Build)}() once!");
            m_Built = true;

            var serviceProvider = ServiceCollection.BuildServiceProvider();

            SetServiceProviders(serviceProvider);

            var service = serviceProvider.GetService<T>();
            return service;
        }

        protected virtual void SetServiceProviders(ServiceProvider serviceProvider) { }
    }
}
