using Ikiru.Parsnips.Api.RocketReach.Models;
using Ikiru.Parsnips.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Refit;
using System;

namespace Ikiru.Parsnips.Api.RocketReach
{
    public static class RocketReachApiStartupExtensions
    {
        public static void AddRocketReachApi(this IServiceCollection services, IConfiguration configuration)
        {
            // Bind settings
            services.Configure<RocketReachSettings>(configuration.GetSection(nameof(RocketReachSettings)));
            
            // Register Refit client
            services.AddRefitClient<IRocketReachApi>()
                    .ConfigureHttpClient((sp, c) => c.BaseAddress = new Uri(sp.GetService<IOptions<RocketReachSettings>>().Value.BaseUrl));
            // TODO: Missing transient retries

            // Service
            services.AddTransient<RocketReachService>();

        }
    }
}
