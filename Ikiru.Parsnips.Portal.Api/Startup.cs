using AutoMapper;
using Ikiru.Parsnips.Application;
using Ikiru.Parsnips.Application.Services.DataPool;
using Ikiru.Parsnips.Application.Shared.Mappings;
using Ikiru.Parsnips.DomainInfrastructure.Startup;
using Ikiru.Parsnips.Infrastructure;
using Ikiru.Parsnips.Portal.Api.Authentication;
using Ikiru.Parsnips.Portal.Api.Extensions;
using Ikiru.Parsnips.Shared.Infrastructure.Chargebee;
using Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi;
using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi;
using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityServerClientAuth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Text.Json.Serialization;

namespace Ikiru.Parsnips.Portal.Api
{
    public class Startup
    {
        private IWebHostEnvironment _environment { get; }
        private IConfiguration _configuration;

        public Startup(IWebHostEnvironment environment, IConfiguration configuration)
        {
            _environment = environment;
            _configuration = configuration;
        }

        public static void ConfigureTestableServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpContextAccessor();
            services.AddSingleton<AuthenticatedUserAccessor>();

            services.AddAutoMapper(cfg =>
            {
                cfg.DisableConstructorMapping();
                cfg.ShouldMapField = _ => false;
                cfg.ShouldMapMethod = _ => false;
                cfg.ForAllMaps((map, expression) => expression.IgnoreAllPropertiesWithAnInaccessibleSetter()); // Disallow Setting values on private setters to preserve rich encapsulated Domain model.
            },
                                   typeof(Startup),
                                   typeof(InfrastructureMappingProfile),
                                   typeof(ApplicationMappingProfile));

            services.Configure<ChargebeeSecuritySettings>(configuration.GetSection(nameof(ChargebeeSecuritySettings)));
       
            services.AddApplicationForPortal(configuration);

            services.AddSingleton<IDataPoolService, DataPoolService>();
            services.AddDataPoolApi(configuration);            
        }


        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {                        
            var bearerTokenAuthSettings = _configuration.GetSection(nameof(BearerTokenAuthSettings)).Get<BearerTokenAuthSettings>();
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
                    {
                        options.Authority = bearerTokenAuthSettings.Authority;
                        options.Audience = bearerTokenAuthSettings.Audience;
                        options.TokenValidationParameters.ClockSkew = TimeSpan.FromMilliseconds(bearerTokenAuthSettings.AllowedClockSkewMs);

                        if (_environment.IsDevelopment())
                            options.RequireHttpsMetadata = false;
                    });

            services.AddMvcCore().AddJsonOptions(opts =>
            {
                var enumConverter = new JsonStringEnumConverter();
                opts.JsonSerializerOptions.Converters.Add(enumConverter);
            });

            services.AddApplicationInsightsTelemetry();
            services.AddCosmos(_configuration);
            services.AddIdentityAdminApi(_configuration);

            ConfigureTestableServices(services, _configuration);

            services.AddSingleton<IdentityServerDiscoveryDocumentRetriever>();

            services.AddApiSwagger(_configuration);
            services.AddControllers();
            services.AddCors(options =>
            {
                options.AddPolicy(
                                  "LocalDev",
                                  policyBuilder =>
                                  {
                                      policyBuilder
                                         .AllowAnyOrigin()
                                         .AllowAnyMethod()
                                         .AllowAnyHeader();
                                  }
                                 );
            });

            services.AddAutoMapper(cfg =>
            {
                cfg.DisableConstructorMapping();
                cfg.ShouldMapField = _ => false;
                cfg.ShouldMapMethod = _ => false;
                cfg.ForAllMaps((map, expression) => expression.IgnoreAllPropertiesWithAnInaccessibleSetter()); // Disallow Setting values on private setters to preserve rich encapsulated Domain model.
            },
                                  typeof(Startup),
                                  typeof(InfrastructureMappingProfile),
                                  typeof(ApplicationMappingProfile));

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseCors("LocalDev");
                app.ApplicationServices.ConfigureCosmosDevelopmentEnvironment();
            }

            app.UseCors("LocalDev");

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseHttpsRedirection();
            app.UseSecureHttpHeaders();

            app.UseApiSwagger();

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers()
                  .RequireAuthorization();
            });
        }
    }
}

