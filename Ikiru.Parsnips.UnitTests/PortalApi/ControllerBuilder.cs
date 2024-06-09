using Ikiru.Parsnips.Portal.Api;
using Ikiru.Parsnips.UnitTests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Ikiru.Parsnips.UnitTests.PortalApi
{
    public class ControllerBuilder<T> : ClassBuilder<T> where T : ControllerBase
    {
        internal HttpContext HttpContext { get; } = new DefaultHttpContext();

        public ControllerBuilder()
        {
            Startup.ConfigureTestableServices(ServiceCollection, new ConfigurationBuilder().AddJsonFile("appsettings.unittest.json").Build());
        }

        public override T Build()
        {
            ServiceCollection.AddSingleton(Mock.Of<IHttpContextAccessor>(a => a.HttpContext == HttpContext));

            var controller = base.Build();

            controller.ControllerContext.HttpContext = HttpContext;
            return controller;
        }

        protected override void SetServiceProviders(ServiceProvider serviceProvider) => HttpContext.RequestServices = serviceProvider;
    }
}
