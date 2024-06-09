using IdentityModel.Client;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityServerClientAuth
{
    public class IdentityServerDiscoveryDocumentRetriever
    {
        private static DiscoveryDocumentResponse s_DiscoveryDocument;

        private static readonly SemaphoreSlim s_Semaphore = new SemaphoreSlim(1, 1);
        
        public async Task<DiscoveryDocumentResponse> GetDiscoveryDocument(HttpClient client)
        {
            // Simple async thread safety
            await s_Semaphore.WaitAsync();
            try
            {
                if (s_DiscoveryDocument != null)
                    return s_DiscoveryDocument;

                var disco = await client.GetDiscoveryDocumentAsync();
                if (disco.IsError)
                    throw new Exception(disco.Error, disco.Exception);

                s_DiscoveryDocument = disco;
                return disco;
            }
            finally
            {
                s_Semaphore.Release();
            }
        }
    }
}