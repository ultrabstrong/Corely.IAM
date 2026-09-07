using Corely.IAM.IntegrationTests.Infrastructure;
using Corely.IAM.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.IntegrationTests.Persistence;

/// <summary>
/// The permitted-id scope is a hand-built expression tree, so it has to be proven against real SQL
/// translation rather than only against the in-memory repo, which happily evaluates any expression
/// that compiles.
/// </summary>
/// <remarks>
/// These run as the group member deliberately. That user reaches the Editor role through a group,
/// and the Editor role carries a grant on one specific role id rather than a wildcard - so the
/// scope is a real id set and the query gets an IN clause. Running as the owner would take the
/// wildcard path, add no clause at all, and prove nothing about translation.
/// </remarks>
public class AuthorizationScopedListTests(IamScenario scenario) : IClassFixture<IamScenario>
{
    [Fact]
    public async Task APerResourceGrant_ListsOnlyTheGrantedRole()
    {
        var result = await ListRolesAsync();

        Assert.Equal(RetrieveResultCode.Success, result.ResultCode);
        Assert.Contains(result.Data!.Items, r => r.Id == scenario.EditorRoleId);
        Assert.DoesNotContain(result.Data.Items, r => r.Id == scenario.ReaderRoleId);
        Assert.DoesNotContain(result.Data.Items, r => r.Id == scenario.UngrantedRoleId);
    }

    [Fact]
    public async Task TotalCountCountsOnlyTheGrantedRoles()
    {
        var result = await ListRolesAsync();

        // The account holds more roles than this. A total that counted them would report pages
        // that do not exist.
        Assert.Equal(result.Data!.Items.Count, result.Data.TotalCount);
    }

    [Fact]
    public async Task PagingAScopedQuery_YieldsExactlyTheCountedRows()
    {
        var total = (await ListRolesAsync()).Data!.TotalCount;

        var walked = 0;
        for (var skip = 0; skip < total; skip++)
        {
            walked += (await ListRolesAsync(skip: skip, take: 1)).Data!.Items.Count;
        }

        Assert.Equal(total, walked);
    }

    [Fact]
    public async Task AWildcardGrant_AddsNoScopeAndListsEverything()
    {
        // The owner takes the null-scope path, which must stay unfiltered.
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
        Assert.Contains(result.Data.Items, r => r.Id == scenario.UngrantedRoleId);
    }

    private Task<RetrieveListResult<Role>> ListRolesAsync(int skip = 0, int take = 100) =>
        scenario.ActAsAsync(
            scenario.GroupMemberUsername,
            scenario.AccountId,
            services =>
                services
                    .GetRequiredService<IRetrievalService>()
                    .ListRolesAsync(
                        new ListRolesRequest(scenario.AccountId, Skip: skip, Take: take)
                    )
        );
}
