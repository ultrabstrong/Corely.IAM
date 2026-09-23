using Corely.IAM.IntegrationTests.Infrastructure;
using Corely.IAM.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.IntegrationTests.Persistence;

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
