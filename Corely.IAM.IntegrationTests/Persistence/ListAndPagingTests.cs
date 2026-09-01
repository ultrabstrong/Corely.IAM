using Corely.IAM.IntegrationTests.Infrastructure;
using Corely.IAM.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.IntegrationTests.Persistence;

public class ListAndPagingTests(IamScenario scenario) : IClassFixture<IamScenario>
{
    [Fact]
    public async Task ListingUsers_ReturnsTheAccountMembers()
    {
        var result = await ListUsersAsync(skip: 0, take: 25);

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Data);
        Assert.Contains(result.Data!.Items, u => u.Id == scenario.DirectMemberUserId);
        Assert.Contains(result.Data.Items, u => u.Id == scenario.GroupMemberUserId);
    }

    [Fact]
    public async Task ListingUsers_ExcludesNonMembers()
    {
        var result = await ListUsersAsync(skip: 0, take: 25);

        Assert.DoesNotContain(result.Data!.Items, u => u.Id == scenario.OutsiderUserId);
    }

    [Fact]
    public async Task TotalCountReflectsTheWholeSetNotThePage()
    {
        var firstPage = await ListUsersAsync(skip: 0, take: 1);

        Assert.Single(firstPage.Data!.Items);
        Assert.True(
            firstPage.Data.TotalCount > 1,
            "TotalCount must count the full result set, not the page."
        );
    }

    [Fact]
    public async Task PagingReturnsDisjointResults()
    {
        var first = await ListUsersAsync(skip: 0, take: 1);
        var second = await ListUsersAsync(skip: 1, take: 1);

        Assert.Single(first.Data!.Items);
        Assert.Single(second.Data!.Items);
        Assert.NotEqual(first.Data.Items[0].Id, second.Data.Items[0].Id);
    }

    [Fact]
    public async Task APageBeyondTheEnd_ReturnsEmptyRatherThanThrowing()
    {
        var result = await ListUsersAsync(skip: 1000, take: 25);

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.Empty(result.Data!.Items);
    }

    [Fact]
    public async Task HasMoreIsFalseOnTheLastPage()
    {
        var all = await ListUsersAsync(skip: 0, take: 100);

        Assert.False(all.Data!.HasMore);
    }

    [Fact]
    public async Task ListingRoles_IsScopedToTheAccount()
    {
        var result = await scenario.ActAsAsync(
            scenario.OwnerUsername,
            scenario.AccountId,
            services =>
                services
                    .GetRequiredService<IRetrievalService>()
                    .ListRolesAsync(new ListRolesRequest(scenario.AccountId, Skip: 0, Take: 100))
        );

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.Contains(result.Data!.Items, r => r.Id == scenario.ReaderRoleId);
        Assert.Contains(result.Data.Items, r => r.Id == scenario.EditorRoleId);
    }

    [Fact]
    public async Task ListingRolesInTheOtherAccount_DoesNotLeakThisAccountsRoles()
    {
        var result = await scenario.ActAsAsync(
            scenario.OwnerUsername,
            scenario.OtherAccountId,
            services =>
                services
                    .GetRequiredService<IRetrievalService>()
                    .ListRolesAsync(
                        new ListRolesRequest(scenario.OtherAccountId, Skip: 0, Take: 100)
                    )
        );

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.DoesNotContain(result.Data!.Items, r => r.Id == scenario.ReaderRoleId);
        Assert.DoesNotContain(result.Data.Items, r => r.Id == scenario.EditorRoleId);
    }

    [Fact]
    public async Task HydratingARole_ReturnsItsGraphWithoutDuplication()
    {
        var result = await scenario.ActAsAsync(
            scenario.OwnerUsername,
            scenario.AccountId,
            services =>
                services
                    .GetRequiredService<IRetrievalService>()
                    .GetRoleAsync(scenario.EditorRoleId, hydrate: true)
        );

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        var role = result.Item!;
        Assert.Equal(role.Groups.Select(g => g.Id).Distinct().Count(), role.Groups.Count);
        Assert.Equal(role.Permissions.Select(p => p.Id).Distinct().Count(), role.Permissions.Count);
    }

    private Task<RetrieveListResult<User>> ListUsersAsync(int skip, int take) =>
        scenario.ActAsAsync(
            scenario.OwnerUsername,
            scenario.AccountId,
            services =>
                services
                    .GetRequiredService<IRetrievalService>()
                    .ListUsersAsync(
                        new ListUsersRequest(scenario.AccountId, Skip: skip, Take: take)
                    )
        );
}
