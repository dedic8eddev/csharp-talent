using Microsoft.AspNetCore.Builder;

namespace Ikiru.Parsnips.Portal.Api.Extensions
{
    public static class StartupSecureHttpHeadersExtensions
    {
        public static void UseSecureHttpHeaders(this IApplicationBuilder app)
        {
            app.Use(async (context, next) =>
            {
                context.Response.Headers.Add("X-Frame-Options", "SAMEORIGIN");
                context.Response.Headers.Add("X-Xss-Protection", "1");
                context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                context.Response.Headers.Add("Referrer-Policy", "no-referrer");
                await next();
            });
        }
    }
}
