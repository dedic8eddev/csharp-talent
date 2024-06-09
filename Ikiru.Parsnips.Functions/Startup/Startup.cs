using AutoMapper;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using ChargeBee.Api;
using Ikiru.Parsnips.Application;
using Ikiru.Parsnips.Application.Services;
using Ikiru.Parsnips.DomainInfrastructure;
using Ikiru.Parsnips.DomainInfrastructure.Startup;
using Ikiru.Parsnips.Functions.Email;
using Ikiru.Parsnips.Functions.Functions.Import;
using Ikiru.Parsnips.Functions.Maps;
using Ikiru.Parsnips.Functions.Parsing;
using Ikiru.Parsnips.Functions.Parsing.Api;
using Ikiru.Parsnips.Functions.Smtp;
using Ikiru.Parsnips.Shared.Infrastructure.Chargebee;
using Ikiru.Parsnips.Shared.Infrastructure.DataPoolApi;
using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Blobs;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues;
using MailKit.Net.Smtp;
using MediatR;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using System;
using System.IO;

[assembly: FunctionsStartup(typeof(Ikiru.Parsnips.Functions.Startup.Startup))]

namespace Ikiru.Parsnips.Functions.Startup
{
    public class Startup : FunctionsStartup
    {
        private static readonly bool s_IsRunningLocally = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID"));

        public override void Configure(IFunctionsHostBuilder builder)
        {
            var configuration = BuildConfiguration();
            ConfigureDevelopmentAppInsights(builder.Services);
            
            builder.Services.AddSingleton<ICosmosRequestTracker, FunctionCosmosRequestTracker>();
            builder.Services.AddCosmos(configuration);

            ConfigureTestableServices(builder.Services, configuration);

            builder.Services.AddSingleton(new BlobServiceClient(configuration.GetValue<string>("AzureWebJobsStorage")));
            builder.Services.AddSingleton(new QueueServiceClient(configuration.GetValue<string>("AzureWebJobsStorage")));

            var settings = configuration.GetSection(nameof(ChargebeeSettings)).Get<ChargebeeSettings>();
            ApiConfig.Configure(settings.SiteName, settings.ApiKey);

            ConfigureDevelopmentEnvironment(builder.Services);
        }

        public static void ConfigureTestableServices(IServiceCollection services, IConfiguration configuration)
        {
            // Registrations for implementations that *can* be executed within unit tests (exclude from here anything which always requires mocking in all unit tests)

            // Settings
            services.Configure<SovrenSettings>(configuration.GetSection(nameof(SovrenSettings)));
            services.Configure<SmtpSendingSettings>(configuration.GetSection(nameof(SmtpSendingSettings)));
            services.Configure<EmailContentSettings>(configuration.GetSection(nameof(EmailContentSettings)));
            services.Configure<AzureMapsSettings>(configuration.GetSection(nameof(AzureMapsSettings)));
            services.Configure<ChargebeeSettings>(configuration.GetSection(nameof(ChargebeeSettings)));

            // Cosmos
            services.AddSingleton<DataStore>();
            services.AddSingleton<DataQuery>();
            services.AddSingleton<ICosmosFeedIteratorProvider, CosmosFeedIteratorProvider>();

            // Storage
            services.AddSingleton<BlobStorage>();
            services.AddSingleton<QueueStorage>();

            services.AddMediatR(typeof(Startup));

            // Automapper
            services.AddAutoMapper(cfg =>
                                   {
                                       cfg.DisableConstructorMapping();
                                       cfg.ShouldMapField = _ => false;
                                       cfg.ShouldMapMethod = _ => false;
                                       cfg.ForAllMaps((map, expression) => expression.IgnoreAllPropertiesWithAnInaccessibleSetter()); // Disallow Setting values on private setters to preserve rich encapsulated Domain model.
                                   },
                                   typeof(Startup));

            // Business Logic
            services.AddSingleton<TrackImport>();
            services.AddSingleton<ParsingService>();
            services.AddSingleton<EmailContentBuilder>();
            services.AddTransient<SmtpSender>();
            services.AddSingleton<ProcessWithSovren>();
            services.AddSingleton<ProcessLinkedInJson>();

            // External Dependencies
            services.AddRefitClient<ISovrenApi>()
                    .ConfigureHttpClient(c => c.BaseAddress = new Uri(configuration.GetValue<string>($"{nameof(SovrenSettings)}:BaseUrl")));
            services.AddRefitClient<IAzureMaps>()
                    .ConfigureHttpClient(c => c.BaseAddress = new Uri(configuration.GetValue<string>($"{nameof(AzureMapsSettings)}:{nameof(AzureMapsSettings.BaseUrl)}")));
            services.AddDataPoolApi(configuration);

            // New stuff
            services.AddApplication(configuration);
           


            services.AddTransient<ISmtpClient>(_ => new SmtpClient()); // Force parameterless ctor

            services.AddIdentityAdminApi(configuration);
        }

        private static IConfiguration BuildConfiguration()
        {
            // MS are working to 'Make it easy to get function working directory and other context'
            // See: https://github.com/Azure/azure-webjobs-sdk-script/issues/803
            // also:https://github.com/Azure/azure-webjobs-sdk/issues/1817
            //      https://github.com/holgerleichsenring/AutofacOnFunctions/issues/3

            var basePath = s_IsRunningLocally
                               ? Directory.GetCurrentDirectory()
                               : "D:\\home\\site\\wwwroot\\";

            var builder = new ConfigurationBuilder()
                         .SetBasePath(basePath)
                         .AddJsonFile("appSettings.json");

            if (s_IsRunningLocally)
                builder.AddJsonFile("appSettings.Development.json");
            
            builder.AddEnvironmentVariables();

            return builder.Build();
        }

        private static void ConfigureDevelopmentEnvironment(IServiceCollection services)
        {
            if (!s_IsRunningLocally)
                return;

            services.ConfigureCosmosDevelopmentEnvironment();
        }
        
        private static void ConfigureDevelopmentAppInsights(IServiceCollection services)
        {
            if (!s_IsRunningLocally)
                return;

            // Functions Runtime on Azure will register AI automatically - https://github.com/MicrosoftDocs/azure-docs/issues/48451
            services.AddApplicationInsightsTelemetry();
            TelemetryDebugWriter.IsTracingDisabled = true;
        }
    }

    
}
