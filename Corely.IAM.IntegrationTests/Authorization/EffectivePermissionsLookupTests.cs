using System.Text.Json;
using Corely.IAM.IntegrationTests.Infrastructure;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.IntegrationTests.Authorization;

public class EffectivePermissionsLookupTests : IAsyncLifetime
{
    private const string INVOICE = IamScenario.INVOICE_RESOURCE_TYPE;

    private readonly IamScenario _scenario = new();
    private readonly Guid _invoiceId = Guid.CreateVersion7();

    public async ValueTask InitializeAsync()
    {
        await _scenario.InitializeAsync();
        await _scenario.ActAsAsync(
            _scenario.OwnerUsername,
            _scenario.OtherAccountId,
            async services =>
            {
                var accountId = _scenario.OtherAccountId;
                var registration = services.GetRequiredService<IRegistrationService>();
                var role = await registration.RegisterRoleAsync(
                    new RegisterRoleRequest("Invoice editor", accountId)
                );
                var permission = await registration.RegisterPermissionAsync(
                    new RegisterPermissionRequest(accountId, INVOICE, _invoiceId, Read: true)
                );
                var assigned = await registration.RegisterPermissionsWithRoleAsync(
                    new RegisterPermissionsWithRoleRequest(
                        [permission.CreatedPermissionId],
                        role.CreatedRoleId,
                        accountId
                    )
                );
                var held = await registration.RegisterRolesWithUserAsync(
                    new RegisterRolesWithUserRequest(
                        [role.CreatedRoleId],
                        _scenario.OwnerUserId,
                        accountId
                    )
                );
                Assert.Equal(CreatePermissionResultCode.Success, permission.ResultCode);
                Assert.Equal(AssignPermissionsToRoleResultCode.Success, assigned.ResultCode);
                Assert.Equal(AssignRolesToUserResultCode.Success, held.ResultCode);
                return true;
            }
        );
    }

    public ValueTask DisposeAsync() => _scenario.DisposeAsync();

    private Task<RetrieveSingleResult<List<EffectivePermission>>> LookUpAsync(
        Guid accountId,
        Guid? resourceId
    ) =>
        _scenario.ActAsAsync(
            _scenario.OwnerUsername,
            accountId,
            services =>
                services
                    .GetRequiredService<IRetrievalService>()
                    .GetCurrentUserEffectivePermissionsAsync(INVOICE, resourceId)
        );

    [Fact]
    public async Task Lookup_ShowsAResourcesOwnGrant_ForAHostTypeInTheAccountThatHoldsIt()
    {
        var result = await LookUpAsync(_scenario.OtherAccountId, _invoiceId);

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.Contains(result.Item!, p => p.ResourceId == _invoiceId && p.Read);
    }

    [Fact]
    public async Task Lookup_LeavesOutGrantsOnOneResource_ForTheTypeAsAWhole()
    {
        var result = await LookUpAsync(_scenario.OtherAccountId, null);

        Assert.NotEmpty(result.Item!);
        Assert.All(result.Item!, p => Assert.Equal(Guid.Empty, p.ResourceId));
    }

    [Fact]
    public async Task Lookup_AnswersAsForAMissingResource_ForAResourceHeldInAnotherAccount()
    {
        var held = await LookUpAsync(_scenario.AccountId, _invoiceId);
        var missing = await LookUpAsync(_scenario.AccountId, Guid.CreateVersion7());

        Assert.DoesNotContain(held.Item!, p => p.ResourceId == _invoiceId);
        Assert.Equal(JsonSerializer.Serialize(missing), JsonSerializer.Serialize(held));
    }

    [Fact]
    public async Task Lookup_IsRefused_ForTheSystemContext()
    {
        var result = await _scenario.AsSystemAsync(services =>
            services
                .GetRequiredService<IRetrievalService>()
                .GetCurrentUserEffectivePermissionsAsync(INVOICE, _invoiceId)
        );

        Assert.Equal(RetrieveResultCode.UnauthorizedError, result.ResultCode);
        Assert.Null(result.Item);
    }
}
