using System;
using System.Threading.Tasks;
using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi.Models;
using Refit;

namespace Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi
{
    [Headers("Authorization: Bearer X")]
    public interface IIdentityAdminApi
    {
        [Post("/api/users/")]
        Task<CreateUserResult> CreateUser([Body] CreateUserRequest request);

        [Put("/api/users/{id}")]
        Task UpdateUser(Guid id, [Body] UpdateUserRequest request);

        [Get("/api/users?emailaddress={emailAddress}")]
        Task<ApiResponse<User>> GetUser(string emailAddress);

        [Delete("/api/users/{id}/deleteunconfirmed")]
        Task DeleteUnconfirmedUser(Guid id);
    }
}