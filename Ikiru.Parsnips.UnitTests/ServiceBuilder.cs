using Ikiru.Parsnips.UnitTests.Helpers;
using Ikiru.Parsnips.Api;
using Microsoft.Extensions.Configuration;

namespace Ikiru.Parsnips.UnitTests
{
    public class ServiceBuilder<T> : ClassBuilder<T> where T : class
    {
        public ServiceBuilder()
        {
            Startup.ConfigureTestableServices(ServiceCollection, new ConfigurationBuilder().AddJsonFile("appsettings.unittest.json").Build());
        }
    }
}
