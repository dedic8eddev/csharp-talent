using Ikiru.Parsnips.IntegrationTests.Helpers;
using Ikiru.Parsnips.IntegrationTests.Helpers.HttpMessages;
using Ikiru.Parsnips.Application.Shared.Models;
using Ikiru.Parsnips.Domain;
using System;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Ikiru.Parsnips.IntegrationTests.Features.Api.Users.Me
{
    [Collection(nameof(IntegrationTestCollection))]
    public class MeTests : IntegrationTestBase, IClassFixture<MeTests.InviteTestsClassFixture>
    {
        private readonly InviteTestsClassFixture m_ClassFixture;
        private readonly Guid _PlanId = Guid.Parse("FA37D0A6-6634-4479-A1AB-CAB921F3FE1E".ToLower());
        private readonly string _subscriptionId = "4BE40EF4-D66C-4FE4-8BB7-9EE878B364ED".ToLower();

        private DateTimeOffset _subscriptionCurrentEndTerm = DateTime.Now.AddDays(-1);
        private readonly Guid _searchFirmId;

        public sealed class InviteTestsClassFixture : IDisposable
        {
            public IntTestServer Server { get; }

            public InviteTestsClassFixture()
            {
                Server = new TestServerBuilder()
                   .Build();
            }

            public void Dispose()
            {
                Server.Dispose();
            }
        }

        public MeTests(IntegrationTestFixture fixture, InviteTestsClassFixture classFixture, ITestOutputHelper output)
            : base(fixture, output)
        {
            m_ClassFixture = classFixture;

            _searchFirmId = m_ClassFixture.Server.Authentication.DefaultUser.SearchFirmId;

            var cosmosClient = m_ClassFixture.Server.GetCosmosContainer("Subscriptions");
            
            cosmosClient.UpsertItemAsync(new ChargebeePlan
            {
                Id = _PlanId,
                PlanId = "MeTestPlanId",
                PlanType = Domain.Enums.PlanType.Connect
            });
                                   
            cosmosClient.UpsertItemAsync(new ChargebeeSubscription(_searchFirmId)
            {
                SubscriptionId = _subscriptionId,
                PlanId = "MeTestPlanId",
                CurrentTermEnd = _subscriptionCurrentEndTerm
            });
        }

        private Ikiru.Parsnips.Domain.Enums.UserRole GetUserRole() => m_ClassFixture.Server.Authentication.DefaultUser.UserRole;

        [Fact]
        public async Task GetShouldRespondOk()
        {
            // Given
            // When
            var webResponse = await m_ClassFixture.Server.Client.GetAsync("/api/users/me");

            var r = new
            {
                UserRole = UserRole.TeamMember,
                IsSubscriptionExpired = false,
                SubscriptionExpired = DateTimeOffset.MinValue,
                PlanType = "",
                SearchFirmId ="",
                PassedInitialLoginForSearchFirm = false
            };

            var response = await webResponse.Content.DeserializeToAnonymousType(r);

            // Then
            Assert.Equal(HttpStatusCode.OK, webResponse.StatusCode);
            Assert.Equal((UserRole)GetUserRole(), response.UserRole);
            Assert.Equal(m_ClassFixture.Server.Authentication.DefaultUser.SearchFirmId.ToString().ToLower(), response.SearchFirmId);
            Assert.False(response.PassedInitialLoginForSearchFirm);
        }

    }
}
