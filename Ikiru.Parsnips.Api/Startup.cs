using AutoMapper;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using FluentValidation;
using FluentValidation.AspNetCore;
using Ikiru.Parsnips.Api.Authentication;
using Ikiru.Parsnips.Api.Development;
using Ikiru.Parsnips.Api.Filters.ApiException;
using Ikiru.Parsnips.Api.Filters.ResourceNotFound;
using Ikiru.Parsnips.Api.Filters.Unauthorized;
using Ikiru.Parsnips.Api.Filters.ValidationFailure;
using Ikiru.Parsnips.Api.ModelBinding;
using Ikiru.Parsnips.Api.Recaptcha;
using Ikiru.Parsnips.Api.RocketReach;
using Ikiru.Parsnips.Api.Security;
using Ikiru.Parsnips.Api.Services;
using Ikiru.Parsnips.Api.Services.ExportCandidates;
using Ikiru.Parsnips.Api.Services.SearchFirmAccountSubscription;
using Ikiru.Parsnips.Api.Swagger;
using Ikiru.Parsnips.Application;
using Ikiru.Parsnips.Application.Command;
using Ikiru.Parsnips.Application.Command.Subscription;
using Ikiru.Parsnips.Application.Command.Subscription.Models;
using Ikiru.Parsnips.Application.Command.Users;
using Ikiru.Parsnips.Application.Command.Users.Models;
using Ikiru.Parsnips.Application.Query;
using Ikiru.Parsnips.Application.Query.Users.Models;
using Ikiru.Parsnips.Application.Services;
using Ikiru.Parsnips.Application.Services.Person.Models;
using Ikiru.Parsnips.Application.Shared.Mappings;
using Ikiru.Parsnips.DomainInfrastructure;
using Ikiru.Parsnips.DomainInfrastructure.Startup;
using Ikiru.Parsnips.Infrastructure;
using Ikiru.Parsnips.Shared.Infrastructure.Chargebee;
using Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi;
using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi;
using Ikiru.Parsnips.Shared.Infrastructure.Storage;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Blobs;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues;
using MediatR;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Logging;
using Morcatko.AspNetCore.JsonMergePatch;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ikiru.Parsnips.Api
{
    public class Startup
    {
        private IWebHostEnvironment Environment { get; }
        private IConfiguration Configuration { get; }

        public Startup(IWebHostEnvironment environment, IConfiguration configuration)
        {
            Environment = environment;
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            if (Environment.IsDevelopment())
                TelemetryDebugWriter.IsTracingDisabled = true;

            services.AddApplicationInsightsTelemetry();
            services.AddCosmos(Configuration);
            services.AddApplicationForPortal(Configuration);

            ConfigureTestableServices(services, Configuration);

            services.AddControllers(opt =>
                                    {
                                        opt.Filters.Add(new ResourceNotFoundExceptionFilter());
                                        opt.Filters.Add(new ParamValidationFailureExceptionFilter());
                                        opt.Filters.Add(new UnauthorizedExceptionFilter());
                                        opt.Filters.Add(new ExternalApiExceptionFilter());

                                        opt.Filters.Add<ActiveSubscriptionPresentAuthorizeFilter>();

                                        opt.ModelBinderProviders.Insert(0, new ExpandListModelBindingProvider());
                                    })
                    .AddFeatureFolders()
                    .AddFluentValidation()
                    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)))
                    .AddSystemTextJsonMergePatch();

            services.AddValidatorsFromAssemblyContaining<Startup>(ServiceLifetime.Singleton);

            var bearerTokenAuthSettings = Configuration.GetSection(nameof(BearerTokenAuthSettings)).Get<BearerTokenAuthSettings>();
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
                    {
                        options.Authority = bearerTokenAuthSettings.Authority;
                        options.Audience = bearerTokenAuthSettings.Audience;
                        options.TokenValidationParameters.ClockSkew = TimeSpan.FromMilliseconds(bearerTokenAuthSettings.AllowedClockSkewMs);

                        if (Environment.IsDevelopment())
                            options.RequireHttpsMetadata = false;
                    });

            services.AddAuthorizationCore(options =>
                                          {
                                              AddPolicy(options, new AdminRequirement());
                                              AddPolicy(options, new OwnerRequirement());
                                              AddPolicy(options, new TeamMemberRequirement());
                                              AddPolicy(options, new PortalRequirement());
                                          });

            services.AddTransient<IAuthorizationHandler, AdminHandler>();
            services.AddTransient<IAuthorizationHandler, OwnerHandler>();
            services.AddTransient<IAuthorizationHandler, TeamMemberHandler>();
            services.AddTransient<IAuthorizationHandler, PortalHandler>();
            services.AddTransient<AuthorizationRoleHelper>();

            services.AddApiSwagger(Configuration);

            if (Environment.IsDevelopment())
            {
                IdentityModelEventSource.ShowPII = true;

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
            }
        }

        private void AddPolicy(AuthorizationOptions options, IPolicyRequirement requirement)
            => options.AddPolicy(requirement.Policy, policy => policy.Requirements.Add(requirement));

        public static void ConfigureTestableServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpContextAccessor();
            services.AddSingleton<AuthenticatedUserAccessor>();

            services.AddMediatR(typeof(Startup));
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
            services.Configure<UserSettings>(configuration.GetSection(nameof(UserSettings)));

            services.AddSingleton<BlobStorage>();
            services.AddSingleton<QueueStorage>();
            services.AddSingleton(new BlobServiceClient(configuration.GetConnectionString("StorageConnection")));
            services.AddSingleton(new QueueServiceClient(configuration.GetConnectionString("StorageConnection")));
            services.AddSingleton(new BlobSasAccessCreator(configuration.GetConnectionString("StorageConnection"), configuration.GetSection(nameof(BlobStorageSasAccessSettings)).Get<BlobStorageSasAccessSettings>()));
            services.AddSingleton<SasAccess>();
            services.AddSingleton<SearchFirmService>();


            services.AddSingleton<Ikiru.Parsnips.Infrastructure.Azure.Storage.Blob.BlobStorage>();
            services.AddSingleton(new Ikiru.Parsnips.Infrastructure.Azure.Storage.Blob.BlobSasAccessCreator(configuration.GetConnectionString("StorageConnection"), configuration.GetSection(nameof(BlobStorageSasAccessSettings)).Get<Ikiru.Parsnips.Infrastructure.Azure.Storage.Blob.BlobStorageSasAccessSettings>()));
            services.AddSingleton<Ikiru.Parsnips.Infrastructure.Azure.Storage.Blob.SasAccess>();

            services.AddSingleton<DataStore>();
            services.AddSingleton<DataQuery>();
            services.AddSingleton<ICosmosFeedIteratorProvider, CosmosFeedIteratorProvider>();

            services.AddScoped<PersonFetcher>();
            services.AddScoped<PersonUniquenessValidator>();
            services.AddScoped<PersonPhotoService>();
            services.AddScoped<PersonDocumentService>();

            services.AddTransient<ExportCandidatesService>();
            services.AddTransient<CsvGenerator>();
            services.AddTransient<CandidatesFetcher>();
            services.AddTransient<DataPoolPersonFetcher>();

            services.AddIdentityAdminApi(configuration);
            services.AddDataPoolApi(configuration);
            services.AddRocketReachApi(configuration);
            services.AddRecaptchaApi(configuration);

            #region IOC that might need refactoring

            services.AddApplication(configuration);
            services.AddInfrastructure(configuration);

            services.AddSingleton<ICommandHandler<CreateSubscriptionRequest, CreateSubscriptionResponse>, CreateSubscriptionCommand>();
            services.AddSingleton<ISubscribeToTrialService, SubscribeToTrialService>();

            services.AddSingleton<ICommandHandler<MakeUserInActiveRequest, MakeUserInActiveResponse>, MakeUserInactiveCommand>();
            services.AddSingleton<ICommandHandler<MakeUserActiveRequest, MakeUserActiveResponse>, MakeUserInactiveCommand>();

            services.AddSingleton<IQueryHandler<GetActiveUsersRequest, GetActiveUsersResponse>,
                                    Application.Query.Users.UserQuery>();

            #endregion
        }

        public static void Configure(IApplicationBuilder app, IWebHostEnvironment env,
                                    ILoggerFactory loggerFactory, ILogger<Startup> logger)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseCors("LocalDev");
                app.ApplicationServices.ConfigureCosmosDevelopmentEnvironment();
                loggerFactory.AddFile("Logs/API-{Date}.txt");
            }
            else
            {
                app.UseHsts();
            }

            app.UseApiSwagger();

            app.UseHttpsRedirection();
            app.UseSecureHttpHeaders();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers()
                         .RequireAuthorization();
            });
        }
    }
}
