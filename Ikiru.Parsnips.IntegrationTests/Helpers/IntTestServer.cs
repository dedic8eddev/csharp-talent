using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Azure.Cosmos;
using System;
using System.Net.Http;
using Ikiru.Parsnips.IntegrationTests.Helpers.Authentication;
using Ikiru.Parsnips.Api;

namespace Ikiru.Parsnips.IntegrationTests.Helpers
{
    public sealed class IntTestServer : IDisposable
    {
        public DefaultIntegrationTestAuthentication Authentication { get; }

        public BlobServiceClient BlobServiceClient { get; }
        public CosmosClient CosmosClient { get; }

        public WebApplicationFactory<Startup> WebApplicationFactory { get; }
        public HttpClient Client { get; }
        public HttpClient UnauthClient { get; }
        
        public IntTestServer(WebApplicationFactory<Startup> webApplicationFactory, HttpClient authClient, HttpClient unauthClient, BlobServiceClient blobServiceClient, CosmosClient cosmosClient, DefaultIntegrationTestAuthentication authentication)
        {
            WebApplicationFactory = webApplicationFactory;
            Client = authClient;
            UnauthClient = unauthClient;
            BlobServiceClient = blobServiceClient;
            CosmosClient = cosmosClient;
            Authentication = authentication;
        }

        public BlobContainerClient GetContainer(string containerName)
        {
            return BlobServiceClient.GetBlobContainerClient(containerName);
        }

        public Container GetCosmosContainer(string containerName)
        {
            return CosmosClient.GetContainer("Parsnips", containerName);
        }

        public void Dispose()
        {
            Client.Dispose();
            WebApplicationFactory.Dispose();
        }
    }
}