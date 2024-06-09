using Ikiru.Parsnips.Api.Controllers.Users.Invite;
using Ikiru.Parsnips.Domain;
using Ikiru.Parsnips.Domain.Enums;
using Ikiru.Parsnips.Shared.Infrastructure.Exceptions.ValidationFailure;
using Ikiru.Parsnips.Shared.Infrastructure.IdentityServer.IdentityAdminApi;
using Ikiru.Parsnips.UnitTests.Helpers;
using Ikiru.Parsnips.UnitTests.Helpers.TestDataSources;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Ikiru.Parsnips.UnitTests.Api.Controllers.Users.Invite
{
    public class GetTests
    {
        private readonly FakeCosmos m_FakeCosmos;
        private readonly Mock<IIdentityAdminApi> m_IdentityServerApi;

        private static readonly Guid s_InviteToken = Guid.NewGuid();
        private readonly Guid m_SearchFirmId;
        private readonly SearchFirmUser m_SearchFirmUser;
        private readonly SearchFirm m_SearchFirm;

        public GetTests()
        {
            m_SearchFirm = new SearchFirm
            {
                Name = "SearchFirmA"
            };
            
            m_SearchFirmId = m_SearchFirm.SearchFirmId;

            m_SearchFirmUser = new SearchFirmUser(m_SearchFirm.SearchFirmId)
            {
                InviteToken = s_InviteToken,
                Status = SearchFirmUserStatus.Invited,
                EmailAddress = "test@test1.com"
            };

            m_FakeCosmos = new FakeCosmos()
                 .EnableContainerInsert<SearchFirm>(FakeCosmos.SearchFirmsContainerName)
                 .EnableContainerLinqQuery(
                    FakeCosmos.SearchFirmsContainerName,
                    m_SearchFirmId.ToString(),
                    () => new List<SearchFirmUser>
                        {
                            m_SearchFirmUser
                        }
                    )
                .EnableContainerLinqQuery(
                    FakeCosmos.SearchFirmsContainerName,
                    m_SearchFirmId.ToString(),
                    () => new List<SearchFirm>
                        {
                            m_SearchFirm
                        }
                    );

            m_IdentityServerApi = new Mock<IIdentityAdminApi>();
        }

        [Fact]
        public async Task GetInviteReturnsCorrectResult()
        {
            // Given
            var controller = CreateController();
            var token = $"{s_InviteToken}|{m_SearchFirmId}";

            var query = new Get.Query
                        {
                Token = token
            };

            // When
            var actionResult = await controller.Get(query);

            // Then
            var result = Assert.IsType<OkObjectResult>(actionResult);
            // ReSharper disable once PossibleNullReferenceException
            var content = (Get.Result)result.Value;

            Assert.Equal(m_SearchFirmUser.EmailAddress, content.InviteEmailAddress);
            Assert.Equal(m_SearchFirm.Name, content.CompanyName);
            Assert.Equal(m_SearchFirmUser.Id, content.Id);
            Assert.Equal(m_SearchFirm.Id, content.SearchFirmId);
        }
        
        public class InvalidTokens : BaseTestDataSource
        {
            protected override IEnumerator<object[]> GetValues()
            {
                yield return new object[] { $"{s_InviteToken}|vvvvvvvvvvv" };
                yield return new object[] { $"asdfasfdasfdas|{Guid.NewGuid()}" };
                yield return new object[] { "" };
                yield return new object[] { $"{Guid.NewGuid()}|" };
                yield return new object[] { $"|{Guid.NewGuid()}" };
            }
        }

        [Theory]
        [ClassData(typeof(InvalidTokens))]
        public async Task GetInviteThrowsParamValidationWhenTokenIsInvalid(string token)
        {
            // Given
            var controller = CreateController();
            var query = new Get.Query
            {
                Token = token
            };
            
            // When
            var result = await Record.ExceptionAsync(() => controller.Get(query));

            // Then
            var exception = Assert.IsType<ParamValidationFailureException>(result);
            var headerError = Assert.Single(exception.ValidationErrors.Where(x => x.Param == "header"));
            var contentError = Assert.Single(exception.ValidationErrors.Where(x => x.Param == "content"));

            Assert.Equal("Invalid invite link", headerError.Errors[0]);
            Assert.Equal("Ask a member of your team to resend your invite link.", contentError.Errors[0]);
        }

        [Fact]
        public async Task GetInviteThrowsParamValidationWhenTokenNotFound()
        {
            // Given
            var controller = CreateController();
            var query = new Get.Query
                        {
                            Token = $"{Guid.NewGuid()}|{m_SearchFirmId}"
                        };
            
            // When
            var result = await Record.ExceptionAsync(() => controller.Get(query));

            // Then
            var exception = Assert.IsType<ParamValidationFailureException>(result);
            var headerError = Assert.Single(exception.ValidationErrors.Where(x => x.Param == "header"));
            var contentError = Assert.Single(exception.ValidationErrors.Where(x => x.Param == "content"));

            Assert.Equal("Invalid invite link", headerError.Errors[0]);
            Assert.Equal("Ask a member of your team to resend your invite link.", contentError.Errors[0]);
        }

        [Fact]
        public async Task GetInviteThrowsParamValidationWhenInviteIsAlreadyCompleted()
        {
            // Given
            m_SearchFirmUser.Status = SearchFirmUserStatus.Complete;
            var controller = CreateController();
            var query = new Get.Query
                        {
                            Token = $"{s_InviteToken}|{m_SearchFirmId}"
                        };
            
            // When
            var result = await Record.ExceptionAsync(() => controller.Get(query));

            // Then
            var exception =  Assert.IsType<ParamValidationFailureException>(result);
            var headerError = Assert.Single(exception.ValidationErrors.Where(x => x.Param == "header"));
            var contentError = Assert.Single(exception.ValidationErrors.Where(x => x.Param == "content"));

            Assert.Equal("This invite link has already been used.", headerError.Errors[0]);
            Assert.Equal("If you don't have an account ask a member of your team to invite you.", contentError.Errors[0]);
        }

        private InviteController CreateController()
        {
            return new ControllerBuilder<InviteController>()
                  .SetFakeCosmos(m_FakeCosmos)
                  .AddTransient(m_IdentityServerApi.Object)
                  .SetFakeRepository(new FakeRepository())
                  .Build();
        }
    }
}
