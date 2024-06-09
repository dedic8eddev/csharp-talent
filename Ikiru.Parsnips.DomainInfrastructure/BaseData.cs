using System;
using System.Collections.Generic;
using Ikiru.Parsnips.Domain;
using Microsoft.Azure.Cosmos;

namespace Ikiru.Parsnips.DomainInfrastructure
{
    public abstract class BaseData
    {
        public static string DatabaseName = "Parsnips";

        public const string PERSONS_CONTAINER = "Persons";
        public const string IMPORTS_CONTAINER = "Imports";
        public const string SEARCH_FIRMS_CONTAINER = "SearchFirms";
        public const string ASSIGNMENTS_CONTAINER = "Assignments";
        public const string PERSON_NOTES_CONTAINER = "Person_Notes";
        public const string CANDIDATES_CONTAINER = "Candidates";
        public const string SUBSCRIPTION_CONTAINER = "Subscriptions";

        private static readonly Dictionary<Type, string> s_ContainerByType = new Dictionary<Type, string>
                                                                             {
                                                                                 { typeof(Person), PERSONS_CONTAINER },
                                                                                 { typeof(Import), IMPORTS_CONTAINER },
                                                                                 { typeof(SearchFirm), SEARCH_FIRMS_CONTAINER },
                                                                                 { typeof(SearchFirmUser), SEARCH_FIRMS_CONTAINER },
                                                                                 { typeof(SearchFirmToken), SEARCH_FIRMS_CONTAINER },
                                                                                 { typeof(Assignment), ASSIGNMENTS_CONTAINER },
                                                                                 { typeof(Note), PERSON_NOTES_CONTAINER },
                                                                                 { typeof(Candidate), CANDIDATES_CONTAINER },
                                                                                 { typeof(ChargebeePlan), SUBSCRIPTION_CONTAINER },
                                                                                 { typeof(ChargebeeSubscription), SUBSCRIPTION_CONTAINER },
                                                                                 { typeof(ChargebeeEvent), SUBSCRIPTION_CONTAINER },
                                                                                 { typeof(ChargebeeAddon), SUBSCRIPTION_CONTAINER },
                                                                             };
        private readonly CosmosClient m_CosmosClient;

        protected BaseData(CosmosClient cosmosClient)
        {
            m_CosmosClient = cosmosClient;
        }

        protected Container GetContainer<T>()
        {
            var containerName = s_ContainerByType[typeof(T)];
            return m_CosmosClient.GetContainer(DatabaseName, containerName);
        }
    }
}