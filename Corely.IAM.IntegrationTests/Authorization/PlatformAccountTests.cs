using Corely.IAM.Accounts.Entities;
using Corely.IAM.Accounts.Models;
using Corely.IAM.IntegrationTests.Infrastructure;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Platform.Models;
using Corely.IAM.Roles.Constants;
using Corely.IAM.Roles.Entities;
using Corely.IAM.Security.Constants;
using Corely.IAM.Services;
using Corely.IAM.TotpAuths.Entities;
using Corely.IAM.Users.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.IntegrationTests.Authorization;

public class PlatformAccountTests : IAsyncLifetime
{
    private readonly IamScenario _scenario = new();
    private Guid _platformAccountId;

    public async ValueTask InitializeAsync()
    {
        await _scenario.InitializeAsync();

        var result = await _scenario.Host.WithScopeAsync(services =>
            services
                .GetRequiredService<IPlatformService>()
                .BootstrapPlatformAsync(
                    new BootstrapPlatformRequest(
                        "Platform",
                        "platform-owner",
                        "platform-owner@example.com"
                    )
                )
        );
        Assert.Equal(BootstrapPlatformResultCode.Success, result.ResultCode);
        _platformAccountId = result.Credentials!.AccountId;
    }

    public ValueTask DisposeAsync() => _scenario.DisposeAsync();

    [Fact]
    public async Task Bootstrap_FlagsAccountAndEnrollsOwnerTwoFactor_ForFirstRun()
    {
        var (isPlatform, totpEnabled) = await _scenario.Host.QueryAsync(async db =>
            (
                await db.Set<AccountEntity>()
                    .Where(a => a.Id == _platformAccountId)
                    .Select(a => a.IsPlatformAccount)
                    .SingleAsync(),
                await db.Set<TotpAuthEntity>()
                    .AnyAsync(t => t.User!.Username == "platform-owner" && t.IsEnabled)
            )
        );

        Assert.True(isPlatform);
        Assert.True(totpEnabled);
    }

    [Fact]
    public async Task Bootstrap_ChangesNothing_ForSecondRun()
    {
        var result = await _scenario.Host.WithScopeAsync(services =>
            services
                .GetRequiredService<IPlatformService>()
                .BootstrapPlatformAsync(
                    new BootstrapPlatformRequest("Again", "second-owner", "second@example.com")
                )
        );

        Assert.Equal(BootstrapPlatformResultCode.AlreadyBootstrappedError, result.ResultCode);
        Assert.Equal(
            1,
            await _scenario.Host.QueryAsync(db =>
                db.Set<AccountEntity>().CountAsync(a => a.IsPlatformAccount)
            )
        );
    }

    [Fact]
    public async Task CompletePlatformOwnerPermissions_GivesFullAccess_ForEveryRegisteredType()
    {
        var fullAccessTypes = await _scenario.Host.QueryAsync(db =>
            db.Set<RoleEntity>()
                .Where(r =>
                    r.AccountId == _platformAccountId && r.Name == RoleConstants.OWNER_ROLE_NAME
                )
                .SelectMany(r => r.Permissions!)
                .Where(p =>
                    p.ResourceId == Guid.Empty
                    && p.Create
                    && p.Read
                    && p.Update
                    && p.Delete
                    && p.Execute
                    && p.IsSystemDefined
                )
                .Select(p => p.ResourceType)
                .ToListAsync()
        );

        Assert.Contains(IamScenario.INVOICE_RESOURCE_TYPE, fullAccessTypes);
        Assert.Contains(IamScenario.REPORT_RESOURCE_TYPE, fullAccessTypes);
        Assert.Contains(PermissionConstants.ACCOUNT_RESOURCE_TYPE, fullAccessTypes);
    }

    [Fact]
    public async Task CompletePlatformOwnerPermissions_AddsNothing_ForCompleteOwner()
    {
        var added = await _scenario.Host.WithScopeAsync(services =>
            services.GetRequiredService<IPlatformService>().CompletePlatformOwnerPermissionsAsync()
        );

        Assert.Equal(0, added);
    }

    [Fact]
    public async Task PlatformMember_EntersAccountTheyDoNotBelongTo_ForAccountRead()
    {
        await GivePlatformRoleAsync((PermissionConstants.ACCOUNT_RESOURCE_TYPE, [AuthAction.Read]));

        var current = await _scenario.ActAsAsync(
            _scenario.OutsiderUsername,
            _scenario.AccountId,
            services =>
                Task.FromResult(
                    services
                        .GetRequiredService<Users.Providers.IUserContextProvider>()
                        .GetUserContext()!
                        .CurrentAccount!.Id
                )
        );

        Assert.Equal(_scenario.AccountId, current);
    }

    [Fact]
    public async Task PlatformMember_IsMarkedAsEntering_AnAccountTheyDoNotBelongTo()
    {
        await GivePlatformRoleAsync((PermissionConstants.ACCOUNT_RESOURCE_TYPE, [AuthAction.Read]));

        var context = await _scenario.ActAsAsync(
            _scenario.OutsiderUsername,
            _scenario.AccountId,
            services =>
                Task.FromResult(
                    services
                        .GetRequiredService<Users.Providers.IUserContextProvider>()
                        .GetUserContext()!
                )
        );

        Assert.True(context.EnteredAsPlatformMember);
        Assert.DoesNotContain(context.MemberAccounts, a => a.Id == _scenario.AccountId);
        Assert.Contains(context.MemberAccounts, a => a.Id == _platformAccountId);
    }

    [Fact]
    public async Task User_CannotEnterAccountTheyDoNotBelongTo_WithoutPlatformAccountRead()
    {
        var switched = await _scenario.Host.WithScopeAsync(async services =>
        {
            var authentication = services.GetRequiredService<IAuthenticationService>();
            await authentication.SignInAsync(
                new SignInRequest(
                    _scenario.OutsiderUsername,
                    IamScenario.Password,
                    "device-outsider"
                )
            );
            return await authentication.SwitchAccountAsync(
                new SwitchAccountRequest(_scenario.AccountId)
            );
        });

        Assert.NotEqual(SignInResultCode.Success, switched.ResultCode);
    }

    [Fact]
    public async Task PlatformMember_ListsEveryAccount_ForAccountRead()
    {
        await GivePlatformRoleAsync((PermissionConstants.ACCOUNT_RESOURCE_TYPE, [AuthAction.Read]));

        var names = await _scenario.ActAsAsync(
            _scenario.OutsiderUsername,
            _platformAccountId,
            async services =>
                (
                    await services
                        .GetRequiredService<IRetrievalService>()
                        .ListAccountsAsync(new ListAccountsRequest(Take: 50))
                ).Data!.Items.Select(a => a.AccountName)
        );

        Assert.Equal(["Platform", "Primary", "Secondary"], names.Order());
    }

    [Fact]
    public async Task PlatformMember_HoldsOnlyPlatformRolePermissions_InAnotherAccount()
    {
        await GivePlatformRoleAsync(
            (PermissionConstants.ACCOUNT_RESOURCE_TYPE, [AuthAction.Read]),
            (IamScenario.INVOICE_RESOURCE_TYPE, [AuthAction.Read, AuthAction.Create])
        );

        Assert.True(
            await _scenario.IsAuthorizedAsync(
                _scenario.OutsiderUsername,
                _scenario.AccountId,
                AuthAction.Create,
                IamScenario.INVOICE_RESOURCE_TYPE
            )
        );
        Assert.False(
            await _scenario.IsAuthorizedAsync(
                _scenario.OutsiderUsername,
                _scenario.AccountId,
                AuthAction.Delete,
                IamScenario.INVOICE_RESOURCE_TYPE
            )
        );
        Assert.False(
            await _scenario.IsAuthorizedAsync(
                _scenario.OutsiderUsername,
                _scenario.AccountId,
                AuthAction.Read,
                PermissionConstants.USER_RESOURCE_TYPE
            )
        );
    }

    [Fact]
    public async Task PlatformMember_SeesAccessComingFromPlatformAccount_InEffectivePermissions()
    {
        await GivePlatformRoleAsync(
            (PermissionConstants.ACCOUNT_RESOURCE_TYPE, [AuthAction.Read]),
            (PermissionConstants.USER_RESOURCE_TYPE, [AuthAction.Read])
        );

        var roles = await _scenario.ActAsAsync(
            _scenario.OutsiderUsername,
            _scenario.AccountId,
            async services =>
                (
                    await services
                        .GetRequiredService<IRetrievalService>()
                        .GetUserAsync(_scenario.OwnerUserId)
                ).EffectivePermissions!.SelectMany(p => p.Roles)
        );

        Assert.Contains(roles, r => r.ViaPlatformAccount);
    }

    [Fact]
    public async Task DeleteAccount_RefusesPlatformAccount_ForSystemContext()
    {
        var result = await _scenario.AsSystemAsync(services =>
            services
                .GetRequiredService<IDeregistrationService>()
                .DeregisterAccountAsync(new DeregisterAccountRequest(_platformAccountId))
        );

        Assert.Equal(DeregisterAccountResultCode.PlatformAccountError, result.ResultCode);
    }

    [Fact]
    public async Task PlatformMember_CannotEnterAccountTheyDoNotBelongTo_WithoutTwoFactor()
    {
        await GivePlatformRoleWithoutTwoFactorAsync(
            (PermissionConstants.ACCOUNT_RESOURCE_TYPE, [AuthAction.Read])
        );

        var switched = await SignInAndSwitchAsync(_scenario.AccountId);

        Assert.Equal(SignInResultCode.TwoFactorRequiredError, switched.ResultCode);
    }

    [Fact]
    public async Task PlatformMember_CannotEnterPlatformAccount_WithoutTwoFactor()
    {
        await GivePlatformRoleWithoutTwoFactorAsync(
            (PermissionConstants.ACCOUNT_RESOURCE_TYPE, [AuthAction.Read])
        );

        var switched = await SignInAndSwitchAsync(_platformAccountId);

        Assert.Equal(SignInResultCode.TwoFactorRequiredError, switched.ResultCode);
    }

    [Fact]
    public async Task PlatformMember_CannotSignInToPlatformAccount_WithoutTwoFactor()
    {
        await GivePlatformRoleWithoutTwoFactorAsync(
            (PermissionConstants.ACCOUNT_RESOURCE_TYPE, [AuthAction.Read])
        );

        var signIn = await _scenario.Host.WithScopeAsync(services =>
            services
                .GetRequiredService<IAuthenticationService>()
                .SignInAsync(
                    new SignInRequest(
                        _scenario.OutsiderUsername,
                        IamScenario.Password,
                        "device-outsider",
                        _platformAccountId
                    )
                )
        );

        Assert.Equal(SignInResultCode.TwoFactorRequiredError, signIn.ResultCode);
    }

    [Fact]
    public async Task PlatformMember_CannotRenewTokenInAnotherAccount_AfterTwoFactorIsTurnedOff()
    {
        await GivePlatformRoleAsync((PermissionConstants.ACCOUNT_RESOURCE_TYPE, [AuthAction.Read]));
        var token = await _scenario.ActAsAsync(
            _scenario.OutsiderUsername,
            _scenario.AccountId,
            services =>
                services
                    .GetRequiredService<IAuthenticationService>()
                    .SwitchAccountAsync(new SwitchAccountRequest(_scenario.AccountId))
        );
        await _scenario.Host.QueryAsync(db =>
            db.Set<TotpAuthEntity>()
                .Where(t => t.UserId == _scenario.OutsiderUserId)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsEnabled, false))
        );

        var renewed = await _scenario.Host.WithScopeAsync(services =>
            services
                .GetRequiredService<IAuthenticationService>()
                .RenewAuthTokenAsync(new RenewAuthTokenRequest(token.AuthToken!))
        );

        Assert.Equal(RenewAuthTokenResultCode.TwoFactorRequiredError, renewed.ResultCode);
    }

    [Fact]
    public async Task AccountMember_EntersTheirOwnAccount_WithoutTwoFactor()
    {
        var switched = await _scenario.Host.WithScopeAsync(async services =>
        {
            var authentication = services.GetRequiredService<IAuthenticationService>();
            await authentication.SignInAsync(
                new SignInRequest(
                    _scenario.DirectMemberUsername,
                    IamScenario.Password,
                    "device-member"
                )
            );
            return await authentication.SwitchAccountAsync(
                new SwitchAccountRequest(_scenario.AccountId)
            );
        });

        Assert.Equal(SignInResultCode.Success, switched.ResultCode);
    }

    private Task<SignInResult> SignInAndSwitchAsync(Guid accountId) =>
        _scenario.Host.WithScopeAsync(async services =>
        {
            var authentication = services.GetRequiredService<IAuthenticationService>();
            var signIn = await authentication.SignInAsync(
                new SignInRequest(
                    _scenario.OutsiderUsername,
                    IamScenario.Password,
                    "device-outsider"
                )
            );
            Assert.Equal(SignInResultCode.Success, signIn.ResultCode);
            return await authentication.SwitchAccountAsync(new SwitchAccountRequest(accountId));
        });

    private async Task GivePlatformRoleAsync(
        params (string ResourceType, AuthAction[] Actions)[] grants
    )
    {
        await GivePlatformRoleWithoutTwoFactorAsync(grants);
        await _scenario.EnrollTwoFactorAsync(_scenario.OutsiderUsername);
    }

    private async Task GivePlatformRoleWithoutTwoFactorAsync(
        params (string ResourceType, AuthAction[] Actions)[] grants
    )
    {
        var userId = _scenario.OutsiderUserId;
        await _scenario.AsSystemAsync(async services =>
        {
            var registration = services.GetRequiredService<IRegistrationService>();
            Assert.Equal(
                RegisterUserWithAccountResultCode.Success,
                (
                    await registration.RegisterUserWithAccountAsync(
                        new RegisterUserWithAccountRequest(userId, _platformAccountId)
                    )
                ).ResultCode
            );
            var role = await registration.RegisterRoleAsync(
                new RegisterRoleRequest("Platform slim role", _platformAccountId)
            );
            var permissionIds = new List<Guid>();
            foreach (var (resourceType, actions) in grants)
            {
                var permission = await registration.RegisterPermissionAsync(
                    new RegisterPermissionRequest(
                        _platformAccountId,
                        resourceType,
                        Guid.Empty,
                        actions.Contains(AuthAction.Create),
                        actions.Contains(AuthAction.Read),
                        actions.Contains(AuthAction.Update),
                        actions.Contains(AuthAction.Delete),
                        actions.Contains(AuthAction.Execute)
                    )
                );
                Assert.Equal(CreatePermissionResultCode.Success, permission.ResultCode);
                permissionIds.Add(permission.CreatedPermissionId);
            }
            await registration.RegisterPermissionsWithRoleAsync(
                new RegisterPermissionsWithRoleRequest(
                    permissionIds,
                    role.CreatedRoleId,
                    _platformAccountId
                )
            );
            await registration.RegisterRolesWithUserAsync(
                new RegisterRolesWithUserRequest([role.CreatedRoleId], userId, _platformAccountId)
            );
            return true;
        });
    }
}
