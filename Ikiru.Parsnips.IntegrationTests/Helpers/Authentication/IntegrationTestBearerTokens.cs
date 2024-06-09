using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Ikiru.Parsnips.IntegrationTests.Helpers.Authentication
{
    /// <summary>
    /// Class to create Bearer Tokens for calling the endpoints in the IntegrationTests.  Provides a configure method that can be applied to the Test Server
    /// to allow these Bearer Tokens to be valid (And avoid the Server under test trying to contact the Identity Server).
    /// </summary>
    public class IntegrationTestBearerTokens
    {
        private const string _AUDIENCE = "ParsnipsApi";
        private const string _AUDIENCEPORTAL = "PortalApi";
        private const int _MINS_VALID = 120;

        private readonly string m_Issuer = $"IntegrationTestsIssuer-{Guid.NewGuid()}";

        // https://stebet.net/mocking-jwt-tokens-in-asp-net-core-integration-tests/
        private readonly RandomNumberGenerator m_Rng = RandomNumberGenerator.Create();
        private readonly byte[] m_Key = new byte[32];

        public SecurityKey SecurityKey { get; }
        public SigningCredentials SigningCredentials { get; }

        public IntegrationTestBearerTokens()
        {
            m_Rng.GetBytes(m_Key);
            SecurityKey = new SymmetricSecurityKey(m_Key) { KeyId = Guid.NewGuid().ToString() };
            SigningCredentials = new SigningCredentials(SecurityKey, SecurityAlgorithms.HmacSha256);
        }

        public string GenerateToken(string searchFirmId, string userId, string identityServerId = "")
        {
            var now = DateTimeOffset.Now;
            var claims = new[]
                         {
                             new Claim("SearchFirmId", searchFirmId),
                             new Claim("UserId", userId),
                             new Claim("IdentityServerId", identityServerId),
                             new Claim("client_id", "ParsnipsWebApp")
                         };

            // Create the JWT and write it to a string
            var jwt = new JwtSecurityToken(
                                           issuer: m_Issuer,
                                           audience: _AUDIENCE,
                                           notBefore: now.DateTime,
                                           expires: now.DateTime.Add(TimeSpan.FromMinutes(_MINS_VALID)),
                                           claims: claims,
                                           signingCredentials: SigningCredentials
                                          );
            var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);

            return encodedJwt;
        }

        public void ConfigureIntegrationTestBearerTokenValidation(IServiceCollection services)
        {
            services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                                                                                         {
                                                                                             var config = new OpenIdConnectConfiguration
                                                                                             {
                                                                                                 Issuer = m_Issuer
                                                                                             };

                                                                                             config.SigningKeys.Add(SecurityKey);
                                                                                             options.Configuration = config;
                                                                                             options.Events = new JwtBearerEvents
                                                                                             {
                                                                                                 OnAuthenticationFailed = context =>
                                                                                                                          {
                                                                                                                              return Task.CompletedTask;
                                                                                                                          }
                                                                                             };
                                                                                         });
        }
    }
}