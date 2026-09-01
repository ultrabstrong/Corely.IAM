using Corely.IAM.DataAccess;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Security.Constants;
using Corely.IAM.Security.Providers;
using Corely.IAM.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.IntegrationTests.Providers;

public abstract class ProviderMatrixTestsBase(ProviderTestHost host) : IAsyncLifetime
{
    private const string Password = "Provider!Pass123";

    private Guid _ownerUserId;
    private Guid _accountId;
    private Guid _roleId;

    protected ProviderTestHost Host { get; } = host;

    public async ValueTask InitializeAsync()
    {
        if (DockerAvailability.UnavailableReason is not null)
            return;

        await Host.InitializeAsync();
        await Host.MigrateAsync();
        await SeedAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (DockerAvailability.UnavailableReason is null)
            await Host.DisposeAsync();
    }

    [RequiresDockerFact]
    public async Task MigrationsApplyCleanly()
    {
        var applied = await Host.QueryAsync(db => db.Database.GetAppliedMigrationsAsync());

        Assert.NotEmpty(applied);
    }

    [RequiresDockerFact]
    public async Task NoPendingMigrationsRemain()
    {
        var pending = await Host.QueryAsync(db => db.Database.GetPendingMigrationsAsync());

        Assert.Empty(pending);
    }

    [RequiresDockerFact]
    public async Task RegistrationAndSignInWorkEndToEnd()
    {
        var result = await Host.WithScopeAsync(services =>
            services
                .GetRequiredService<IAuthenticationService>()
                .SignInAsync(new SignInRequest("provider-owner", Password, "device"))
        );

        Assert.Equal(SignInResultCode.Success, result.ResultCode);
    }

    [RequiresDockerFact]
    public async Task DeletingARoleWithPermissions_DoesNotViolateForeignKeys()
    {
        var result = await ActAsOwnerAsync(services =>
            services
                .GetRequiredService<IDeregistrationService>()
                .DeregisterRoleAsync(new DeregisterRoleRequest(_roleId, _accountId))
        );

        Assert.Equal(DeregisterRoleResultCode.Success, result.ResultCode);
    }

    [RequiresDockerFact]
    public async Task SetBasedUpdatesTranslate()
    {
        await Host.WithScopeAsync(async services =>
        {
            var result = await services
                .GetRequiredService<IAuthenticationService>()
                .SignInAsync(new SignInRequest("provider-owner", Password, "second-device"));
            Assert.Equal(SignInResultCode.Success, result.ResultCode);
        });

        await ActAsOwnerAsync(async services =>
        {
            await services.GetRequiredService<IAuthenticationService>().SignOutAllAsync();
            return true;
        });

        var active = await Host.QueryAsync(db =>
            db.UserAuthTokens.AsNoTracking()
                .CountAsync(t => t.UserId == _ownerUserId && t.RevokedUtc == null)
        );

        Assert.Equal(0, active);
    }

    [RequiresDockerFact]
    public async Task AuthorizationResolvesAgainstRealProviderSql()
    {
        var authorized = await ActAsOwnerAsync(services =>
            services
                .GetRequiredService<IAuthorizationProvider>()
                .IsAuthorizedAsync(AuthAction.Read, PermissionConstants.ROLE_RESOURCE_TYPE, _roleId)
        );

        Assert.True(authorized, "The account owner must resolve as authorized on any provider.");
    }

    [RequiresDockerFact]
    public async Task NullableComparisonsTranslate()
    {
        var exception = await Record.ExceptionAsync(() =>
            Host.QueryAsync(db =>
                db.UserAuthTokens.AsNoTracking()
                    .Where(t => t.AccountId == _accountId && t.RevokedUtc == null)
                    .ToListAsync()
            )
        );

        Assert.Null(exception);
    }

    private async Task SeedAsync()
    {
        _ownerUserId = await Host.WithScopeAsync(async services =>
        {
            var result = await services
                .GetRequiredService<IRegistrationService>()
                .RegisterUserAsync(
                    new RegisterUserRequest("provider-owner", "owner@example.com", Password)
                );
            Assert.Equal(RegisterUserResultCode.Success, result.ResultCode);
            return result.CreatedUserId;
        });

        _accountId = await Host.WithScopeAsync(async services =>
        {
            var signIn = await services
                .GetRequiredService<IAuthenticationService>()
                .SignInAsync(new SignInRequest("provider-owner", Password, "seed-device"));
            Assert.Equal(SignInResultCode.Success, signIn.ResultCode);

            var account = await services
                .GetRequiredService<IRegistrationService>()
                .RegisterAccountAsync(new RegisterAccountRequest("Provider", _ownerUserId));
            Assert.Equal(RegisterAccountResultCode.Success, account.ResultCode);
            return account.CreatedAccountId;
        });

        _roleId = await ActAsOwnerAsync(async services =>
        {
            var registration = services.GetRequiredService<IRegistrationService>();

            var role = await registration.RegisterRoleAsync(
                new RegisterRoleRequest("Provider Role", _accountId)
            );
            Assert.Equal(CreateRoleResultCode.Success, role.ResultCode);

            var permission = await registration.RegisterPermissionAsync(
                new RegisterPermissionRequest(
                    _accountId,
                    PermissionConstants.USER_RESOURCE_TYPE,
                    Guid.Empty,
                    Read: true
                )
            );
            Assert.Equal(CreatePermissionResultCode.Success, permission.ResultCode);

            var assignment = await registration.RegisterPermissionsWithRoleAsync(
                new RegisterPermissionsWithRoleRequest(
                    [permission.CreatedPermissionId],
                    role.CreatedRoleId,
                    _accountId
                )
            );
            Assert.Equal(AssignPermissionsToRoleResultCode.Success, assignment.ResultCode);

            return role.CreatedRoleId;
        });
    }

    private Task<T> ActAsOwnerAsync<T>(Func<IServiceProvider, Task<T>> work) =>
        Host.WithScopeAsync(async services =>
        {
            var authentication = services.GetRequiredService<IAuthenticationService>();
            var signIn = await authentication.SignInAsync(
                new SignInRequest("provider-owner", Password, "act-device")
            );
            Assert.Equal(SignInResultCode.Success, signIn.ResultCode);

            var switched = await authentication.SwitchAccountAsync(
                new SwitchAccountRequest(_accountId)
            );
            Assert.Equal(SignInResultCode.Success, switched.ResultCode);

            return await work(services);
        });
}

public class MsSqlProviderMatrixTests()
    : ProviderMatrixTestsBase(new ProviderTestHost(DatabaseProvider.MsSql)) { }

public class MySqlProviderMatrixTests()
    : ProviderMatrixTestsBase(new ProviderTestHost(DatabaseProvider.MySql)) { }
