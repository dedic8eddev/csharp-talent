using System;
using System.Linq.Expressions;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Language.Flow;
using Moq.Protected;

namespace Ikiru.Parsnips.UnitTests.IdentityAdminApi
{
    public static class MockDelegatingHandlerExtensions
    {
        public static ISetup<DelegatingHandler, Task<HttpResponseMessage>> Setup(this Mock<DelegatingHandler> mock, Expression<Func<IDelegatingHandlerMethods, Task<HttpResponseMessage>>> expression)
        {
            return mock.Protected().As<IDelegatingHandlerMethods>()
                       .Setup(expression);
        }

        public static void Verify(this Mock<DelegatingHandler> mock, Expression<Func<IDelegatingHandlerMethods, Task<HttpResponseMessage>>> expression)
        {
            mock.Protected().As<IDelegatingHandlerMethods>()
                .Verify(expression);
        }

        public static void Verify(this Mock<DelegatingHandler> mock, Expression<Func<IDelegatingHandlerMethods, Task<HttpResponseMessage>>> expression, Times? times)
        {
            mock.Protected().As<IDelegatingHandlerMethods>()
                .Verify(expression, times);
        }

        // This mimics the signature of the Protected method SendAsync in the DelegatingHandler that we are mocking.
        public interface IDelegatingHandlerMethods
        {
            Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
        }
    }
}