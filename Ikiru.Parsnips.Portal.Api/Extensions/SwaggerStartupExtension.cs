using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Portal.Api.Extensions
{
    public static class SwaggerStartupExtension
    {
        private static bool s_EnableSwagger;

        public static IServiceCollection AddApiSwagger(this IServiceCollection services, IConfiguration configuration)
        {
            s_EnableSwagger = configuration.GetValue<bool>("EnableSwagger");
            if (!s_EnableSwagger)
                return services;

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "Portal API", Version = "v1" });
                options.CustomSchemaIds(x => x.FullName);
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                                                                          {
                                                                              {
                                                                                  new OpenApiSecurityScheme
                                                                                  {
                                                                                      Reference = new OpenApiReference
                                                                                                  {
                                                                                                      Type = ReferenceType.SecurityScheme,
                                                                                                      Id = "Bearer"
                                                                                                  },
                                                                                      Scheme = "oauth2",
                                                                                      Name = "Bearer",
                                                                                      In = ParameterLocation.Header,

                                                                                  },
                                                                                  new List<string>()
                                                                              }
                                                                          });
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Access token, example : Bearer eyJhbGciOiJSUzI...",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });
            });

            return services;
        }

        public static IApplicationBuilder UseApiSwagger(this IApplicationBuilder app)
        {
            if (!s_EnableSwagger)
                return app;

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "API V1");
            });

            return app;
        }
    }
}
