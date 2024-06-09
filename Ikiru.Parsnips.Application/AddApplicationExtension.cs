using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Ikiru.Parsnips.Api.Search.LocalSimulator;
using Ikiru.Parsnips.Application.Command;
using Ikiru.Parsnips.Application.Command.Models;
using Ikiru.Parsnips.Application.Command.Subscription;
using Ikiru.Parsnips.Application.Command.Subscription.Models;
using Ikiru.Parsnips.Application.Command.Users;
using Ikiru.Parsnips.Application.Command.Users.Models;
using Ikiru.Parsnips.Application.Infrastructure.Location;
using Ikiru.Parsnips.Application.Infrastructure.Location.Models;
using Ikiru.Parsnips.Application.Infrastructure.Subscription;
using Ikiru.Parsnips.Application.Persistence;
using Ikiru.Parsnips.Application.Query;
using Ikiru.Parsnips.Application.Query.Subscription;
using Ikiru.Parsnips.Application.Query.Subscription.Models;
using Ikiru.Parsnips.Application.Query.Users;
using Ikiru.Parsnips.Application.Query.Users.Models;
using Ikiru.Parsnips.Application.Search;
using Ikiru.Parsnips.Application.Services;
using Ikiru.Parsnips.Application.Services.Assignment;
using Ikiru.Parsnips.Application.Services.DataPool;
using Ikiru.Parsnips.Application.Services.Person;
using Ikiru.Parsnips.Application.Services.PortalUser;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Notes;
using Ikiru.Parsnips.Shared.Infrastructure.Search;
using Ikiru.Parsnips.Shared.Infrastructure.Search.Configuration;
using Ikiru.Parsnips.Shared.Infrastructure.Search.Pagination;
using Ikiru.Parsnips.Shared.Infrastructure.Storage;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Blobs;
using Ikiru.Parsnips.Shared.Infrastructure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace Ikiru.Parsnips.Application
{
    public static class AddApplicationExtension
    {
        public static IServiceCollection AddApplicationForPortal(
             this IServiceCollection services, IConfiguration configuration)
        {
            services.AddPersistence(GetPersistenceConfig(configuration));

            services.AddSingleton<CandidateRepository>();
            services.AddSingleton<NoteRepository>();
            services.AddSingleton<AssignmentRepository>();
            services.AddSingleton<PortalUserRepository>();
            services.AddTransient<SearchFirmRepository>();
            services.AddSingleton<UserRepository>();

            services.AddSingleton<IAssignmentNoteService, AssignmentService>();
            services.AddSingleton<IAssignmentService, AssignmentService>();
            services.AddSingleton<INoteService, NoteService>();
            services.AddSingleton<PersonRepository>();
            services.AddSingleton<CandidateServices>();
            services.AddSingleton<PortalUserService>();

            return services;
        }

            public static IServiceCollection AddApplication(
             this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<ICommandHandler<PersonScrapedRequest, CommandResponse<PersonScrapedResponse>>, PersonScrapedCommand>();
            services.AddSingleton<PersonRepository>();
            services.AddSingleton<NoteRepository>();
            services.AddSingleton<AssignmentRepository>();
            services.AddSingleton<SubscriptionRepository>();
            services.AddSingleton<AddonRepository>();
            services.AddSingleton<CouponRepository>();
            services.AddSingleton<UserRepository>();
            services.AddSingleton<TokenRepository>();
            services.AddSingleton<CandidateRepository>();
            services.AddSingleton<PortalUserRepository>();
            services.AddTransient<SearchFirmRepository>();
            services.AddTransient<IQueryHandler<PlanRequest, PlanResponse>, PlanQuery>();
            services.AddSingleton<ICommandHandler<PersonScrapedRequest, CommandResponse<PersonScrapedResponse>>, PersonScrapedCommand>();
            services.AddSingleton<IQueryHandler<GetUserDetailsByUserIdRequest, GetUserDetailsResponse>, GetUserDetailsByUserIdQuery>();
            services.AddSingleton<IQueryHandler<EstimateRequest, EstimateResponse>, EstimateQuery>();
            services.AddSingleton<ICommandHandler<CreatePaymentIntentRequest, CreatePaymentIntentResponse>, CreatePaymentIntentCommand>();
            services.AddSingleton<ICommandHandler<UpdateAllPlansRequest, UpdateAllPlansResponse>, UpdateAllPlansCommand>();
            services.AddSingleton<ICommandHandler<UpdateAllAddonsRequest, UpdateAllAddonsResponse>, UpdateAllAddonsCommand>();
            services.AddSingleton<ICommandHandler<UpdateAllCouponsRequest, UpdateAllCouponsResponse>, UpdateAllCouponsCommand>();
            services.AddSingleton<CurrentSubscriptionDetails>();
            services.AddSingleton<CandidateServices>();

            services.AddSingleton<INoteService, NoteService>();


            services.AddSingleton<IPersonService, PersonService>();
            services.AddSingleton<IPersonNoteService, PersonService>();

            services.Configure<AzureMapsSettings>(configuration.GetSection(nameof(AzureMapsSettings)));

            var azureMapsSettings = configuration.GetSection(nameof(AzureMapsSettings)).Get<AzureMapsSettings>();
            services.AddHttpClient<ILocationsAutocompleteClient, LocationsAutocompleteClient>(nameof(LocationsAutocompleteClient),
                                                                    client => client.BaseAddress = new Uri(azureMapsSettings.BaseAddress));
            services.AddSingleton<ILocationsAutocompleteService, LocationsAutocompleteService>();

            services.AddSingleton<InviteProcessor>();
            services.AddSingleton<ISearchFirmTokenProcessor, SearchFirmTokenProcessor>();

            services.AddPersistence(GetPersistenceConfig(configuration));

            services.AddSingleton<ICommandHandler<MakeUserInActiveRequest, MakeUserInActiveResponse>, MakeUserInactiveCommand>();
            services.AddSingleton<ICommandHandler<MakeUserActiveRequest, MakeUserActiveResponse>, MakeUserInactiveCommand>();

            services.AddSingleton<IQueryHandler<GetActiveUsersRequest, GetActiveUsersResponse>,
                                    Application.Query.Users.UserQuery>();

            var storageConnectionString = configuration.GetConnectionString("StorageConnection");
            if (storageConnectionString != null)
            {
                services.AddSingleton<BlobStorage>();
                services.AddSingleton<QueueStorage>();
                services.AddSingleton(new BlobServiceClient(storageConnectionString));
                services.AddSingleton(new QueueServiceClient(storageConnectionString));
                services.AddSingleton(new BlobSasAccessCreator(storageConnectionString,
                                        configuration.GetSection(nameof(BlobStorageSasAccessSettings))
                                         .Get<BlobStorageSasAccessSettings>()));
            }

            var azureSearchSettings = configuration.GetSection(nameof(AzureSearchSettings)).Get<AzureSearchSettings>();
            services.AddSingleton<SearchPerson>();
            services.AddSingleton<ISearchPaginationService, SearchPaginationService>();

            if (azureSearchSettings != null && !string.IsNullOrEmpty(azureSearchSettings.QueryAPIKey))
            {
                services.AddAzureSearch(azureSearchSettings);
                services.AddSingleton<ISearchPersonSdk, SearchPersonSdk>();
            }
            else
            {
                services.AddSingleton<ISearchPersonSdk, SimulatedDocOpsCosmosQueryPersonByName>();
            }

            services.AddSingleton<IDataPoolService, DataPoolService>();

            services.AddSingleton<SasAccess>();
            services.AddSingleton<SearchFirmService>();

            return services;
        }

        private enum Containers
        {
            Persons,
            Candidates,
            Assignments,
            Person_Notes,
            SearchFirms,
            Subscriptions,
            Notes
        };

        private static IConfiguration GetPersistenceConfig(IConfiguration currentConfig)
        {

            //Note: Persistence configuration can be set in the config file if required
            //    "Persistence": {
            //        "Mappings": [
            //          {
            //            "Type": "Person",
            //            "Container": "Persons",
            //            "PartitionKey": "SearchFirmId"
            //          }
            //        ]
            //      }

            Dictionary<string, string> persistenceMapping = new Dictionary<string, string>() {
                { "Persistence:Database","Parsnips" },
                { "Persistence:Mappings:0:Type", nameof(Person) },
                { "Persistence:Mappings:0:Container", Containers.Persons.ToString() },
                { "Persistence:Mappings:0:PartitionKey", nameof(Person.SearchFirmId) },
                { "Persistence:Mappings:0:PartitionField", nameof(Person.SearchFirmId)},
                { "Persistence:Mappings:1:Type", nameof(Candidate) },
                { "Persistence:Mappings:1:Container", Containers.Candidates.ToString() },
                { "Persistence:Mappings:1:PartitionKey", nameof(Candidate.SearchFirmId) },
                { "Persistence:Mappings:1:PartitionField", nameof(Candidate.SearchFirmId)},
                { "Persistence:Mappings:2:Type", nameof(Assignment) },
                { "Persistence:Mappings:2:Container", Containers.Assignments.ToString() },
                { "Persistence:Mappings:2:PartitionKey", nameof(Assignment.SearchFirmId) },
                { "Persistence:Mappings:2:PartitionField", nameof(Assignment.SearchFirmId)},
                { "Persistence:Mappings:3:Type", nameof(Ikiru.Parsnips.Domain.Note) },
                { "Persistence:Mappings:3:Container", Containers.Person_Notes.ToString() },
                { "Persistence:Mappings:3:PartitionKey", nameof(Ikiru.Parsnips.Domain.Note.SearchFirmId) },
                { "Persistence:Mappings:3:PartitionField", nameof(Ikiru.Parsnips.Domain.Note.SearchFirmId)},
                { "Persistence:Mappings:4:Type", nameof(SearchFirm) },
                { "Persistence:Mappings:4:Container", Containers.SearchFirms.ToString() },
                { "Persistence:Mappings:4:PartitionKey", nameof(SearchFirm.SearchFirmId) },
                { "Persistence:Mappings:4:PartitionField", nameof(SearchFirm.SearchFirmId)},
                { "Persistence:Mappings:5:Type", nameof(SearchFirmUser) },
                { "Persistence:Mappings:5:Container", Containers.SearchFirms.ToString() },
                { "Persistence:Mappings:5:PartitionKey", nameof(SearchFirmUser.SearchFirmId) },
                { "Persistence:Mappings:5:PartitionField", nameof(SearchFirmUser.SearchFirmId)},
                { "Persistence:Mappings:6:Type", nameof(SearchFirmToken) },
                { "Persistence:Mappings:6:Container", Containers.SearchFirms.ToString() },
                { "Persistence:Mappings:6:PartitionKey", nameof(SearchFirmUser.SearchFirmId) },
                { "Persistence:Mappings:6:PartitionField", nameof(SearchFirmUser.SearchFirmId)},
                { "Persistence:Mappings:7:Type", nameof(ChargebeePlan) },
                { "Persistence:Mappings:7:Container", Containers.Subscriptions.ToString() },
                { "Persistence:Mappings:7:PartitionKey", nameof(ChargebeePlan.PartitionKey) },
                { "Persistence:Mappings:7:PartitionField", nameof(ChargebeePlan.PartitionKey)},
                { "Persistence:Mappings:8:Type", nameof(ChargebeeAddon) },
                { "Persistence:Mappings:8:Container", Containers.Subscriptions.ToString() },
                { "Persistence:Mappings:8:PartitionKey", nameof(ChargebeeAddon.PartitionKey) },
                { "Persistence:Mappings:8:PartitionField", nameof(ChargebeeAddon.PartitionKey)},
                { "Persistence:Mappings:9:Type", nameof(ChargebeeCoupon) },
                { "Persistence:Mappings:9:Container", Containers.Subscriptions.ToString() },
                { "Persistence:Mappings:9:PartitionKey", nameof(ChargebeeCoupon.PartitionKey) },
                { "Persistence:Mappings:9:PartitionField", nameof(ChargebeeCoupon.PartitionKey)},
                { "Persistence:Mappings:10:Type", nameof(ChargebeeEvent) },
                { "Persistence:Mappings:10:Container", Containers.Subscriptions.ToString() },
                { "Persistence:Mappings:10:PartitionKey", nameof(ChargebeeEvent.PartitionKey) },
                { "Persistence:Mappings:10:PartitionField", nameof(ChargebeeEvent.PartitionKey)},
                { "Persistence:Mappings:11:Type", nameof(ChargebeeSubscription) },
                { "Persistence:Mappings:11:Container", Containers.Subscriptions.ToString() },
                { "Persistence:Mappings:11:PartitionKey", nameof(ChargebeeSubscription.PartitionKey)},
                { "Persistence:Mappings:11:PartitionField", nameof(ChargebeeSubscription.PartitionKey)},
                { "Persistence:Mappings:12:Type", nameof(PortalUser) },
                { "Persistence:Mappings:12:Container", Containers.SearchFirms.ToString() },
                { "Persistence:Mappings:12:PartitionKey", nameof(PortalUser.SearchFirmId) },
                { "Persistence:Mappings:12:PartitionField", nameof(PortalUser.SearchFirmId) },

                { "Persistence:Mappings:13:Type", nameof(AssignmentNote) },
                { "Persistence:Mappings:13:Container", Containers.Notes.ToString() },
                { "Persistence:Mappings:13:PartitionKey", nameof(AssignmentNote.SearchFirmId) },
                { "Persistence:Mappings:13:PartitionField", nameof(AssignmentNote.SearchFirmId) },

                { "Persistence:Mappings:14:Type", nameof(PersonNote) },
                { "Persistence:Mappings:14:Container", Containers.Notes.ToString() },
                { "Persistence:Mappings:14:PartitionKey", nameof(PersonNote.SearchFirmId) },
                { "Persistence:Mappings:14:PartitionField", nameof(PersonNote.SearchFirmId) }




            };
            return new ConfigurationBuilder().AddConfiguration(currentConfig).AddInMemoryCollection(persistenceMapping).Build();
        }

    }
}
