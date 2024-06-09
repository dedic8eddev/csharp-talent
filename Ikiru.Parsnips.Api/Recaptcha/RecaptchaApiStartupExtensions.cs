using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ikiru.Parsnips.Api.RocketReach;
using Ikiru.Parsnips.Api.RocketReach.Models;
using Ikiru.Parsnips.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Refit;

namespace Ikiru.Parsnips.Api.Recaptcha
{
    public static class RecaptchaApiStartupExtensions
    {  
        public static void AddRecaptchaApi(this IServiceCollection services, IConfiguration configuration)
        {
            // Bind settings
            services.Configure<RecaptchaSettings>(configuration.GetSection(nameof(RecaptchaSettings)));
            
            // Register Refit client
            services.AddRefitClient<IRecaptchaApi>()
                    .ConfigureHttpClient((sp, c) =>
                                         {
                                             c.BaseAddress = new Uri(sp.GetService<IOptions<RecaptchaSettings>>().Value.BaseUrl);
                                         });
            // TODO: Missing transient retries

            // Service
            services.AddTransient<RecaptchaService>();

        }
    }
}
