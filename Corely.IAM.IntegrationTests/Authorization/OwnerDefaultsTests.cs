using Corely.IAM.IntegrationTests.Infrastructure;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Security.Constants;

namespace Corely.IAM.IntegrationTests.Authorization;

public class OwnerDefaultsTests : IAsyncLifetime
{
    private readonly IamScenario _scenario = new();

    public ValueTask InitializeAsync() => _scenario.InitializeAsync();

    public ValueTask DisposeAsync() => _scenario.DisposeAsync();

    public static TheoryData<string, AuthAction> IamTypesAndActions()
    {
        var data = new TheoryData<string, AuthAction>();
        foreach (
            var type in new[]
            {
                PermissionConstants.ACCOUNT_RESOURCE_TYPE,
                PermissionConstants.USER_RESOURCE_TYPE,
                PermissionConstants.GROUP_RESOURCE_TYPE,
                PermissionConstants.ROLE_RESOURCE_TYPE,
                PermissionConstants.PERMISSION_RESOURCE_TYPE,
            }
        )
        foreach (var action in Enum.GetValues<AuthAction>())
            data.Add(type, action);
        return data;
    }

    [Theory]
    [MemberData(nameof(IamTypesAndActions))]
    public async Task Owner_HoldsEveryAction_ForEachIamType(string resourceType, AuthAction action)
    {
        Assert.True(
            await _scenario.IsAuthorizedAsync(
                _scenario.OwnerUsername,
                _scenario.AccountId,
                action,
                resourceType
            )
        );
    }

    [Theory]
    [InlineData(AuthAction.Create)]
    [InlineData(AuthAction.Read)]
    [InlineData(AuthAction.Update)]
    [InlineData(AuthAction.Delete)]
    [InlineData(AuthAction.Execute)]
    public async Task Owner_HoldsNothing_ForHostTypeWithoutOwnerActions(AuthAction action)
    {
        Assert.False(
            await _scenario.IsAuthorizedAsync(
                _scenario.OwnerUsername,
                _scenario.AccountId,
                action,
                IamScenario.REPORT_RESOURCE_TYPE
            )
        );
    }

    [Theory]
    [InlineData(AuthAction.Create, false)]
    [InlineData(AuthAction.Read, true)]
    [InlineData(AuthAction.Update, false)]
    [InlineData(AuthAction.Delete, false)]
    [InlineData(AuthAction.Execute, false)]
    public async Task Owner_HoldsExactlyTheRegisteredActions_ForHostTypeWithOwnerActions(
        AuthAction action,
        bool expected
    )
    {
        Assert.Equal(
            expected,
            await _scenario.IsAuthorizedAsync(
                _scenario.OwnerUsername,
                _scenario.AccountId,
                action,
                IamScenario.INVOICE_RESOURCE_TYPE
            )
        );
    }
}
