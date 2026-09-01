using Corely.IAM.IntegrationTests.Infrastructure;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Security.Constants;
using Corely.IAM.Security.Providers;
using Corely.IAM.Services;
using Corely.IAM.TotpAuths.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.IntegrationTests.Authorization;

public class SystemContextTests(IamScenario scenario) : IClassFixture<IamScenario>
{
    [Fact]
    public async Task SystemContext_BypassesPermissionChecks()
    {
        var authorized = await scenario.AsSystemAsync(services =>
            services
                .GetRequiredService<IAuthorizationProvider>()
                .IsAuthorizedAsync(
                    AuthAction.Delete,
                    PermissionConstants.ROLE_RESOURCE_TYPE,
                    scenario.UngrantedRoleId
                )
        );

        Assert.True(authorized);
    }

    [Fact]
    public async Task SystemContext_IsNotANonSystemUserContext()
    {
        var isNonSystem = await scenario.AsSystemAsync(services =>
            Task.FromResult(
                services.GetRequiredService<IAuthorizationProvider>().IsNonSystemUserContext()
            )
        );

        Assert.False(isNonSystem, "System context must not pass the self-operation gate.");
    }

    [Fact]
    public async Task ARealUser_IsANonSystemUserContext()
    {
        var isNonSystem = await scenario.ActAsAsync(
            scenario.OwnerUsername,
            scenario.AccountId,
            services =>
                Task.FromResult(
                    services.GetRequiredService<IAuthorizationProvider>().IsNonSystemUserContext()
                )
        );

        Assert.True(isNonSystem);
    }

    [Fact]
    public async Task SystemContext_IsBlockedFromSelfOperations()
    {
        var result = await scenario.AsSystemAsync(services =>
            services.GetRequiredService<IMfaService>().EnableTotpAsync()
        );

        Assert.Equal(EnableTotpResultCode.UnauthorizedError, result.ResultCode);
    }

    [Fact]
    public async Task ARealUser_IsAllowedSelfOperations()
    {
        var result = await scenario.ActAsAsync(
            scenario.OutsiderUsername,
            null,
            services => services.GetRequiredService<IMfaService>().EnableTotpAsync()
        );

        Assert.Equal(EnableTotpResultCode.Success, result.ResultCode);
    }

    [Fact]
    public async Task SystemContext_HasAUserContext()
    {
        var hasContext = await scenario.AsSystemAsync(services =>
            Task.FromResult(services.GetRequiredService<IAuthorizationProvider>().HasUserContext())
        );

        Assert.True(hasContext, "System context is still a context; only self operations differ.");
    }
}
