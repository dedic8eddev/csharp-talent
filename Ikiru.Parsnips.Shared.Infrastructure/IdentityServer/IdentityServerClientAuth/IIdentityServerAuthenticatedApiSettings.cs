namespace Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityServerClientAuth
{
    public interface IIdentityServerAuthenticatedApiSettings
    {
        string AuthServerBaseUrl { get; }
        string BaseUrl { get; }
        string ClientId { get; }
        string ClientSecret { get; }
        string Scope { get; }
    }
}