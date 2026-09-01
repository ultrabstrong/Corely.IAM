using Corely.IAM.IntegrationTests.Infrastructure;

namespace Corely.IAM.IntegrationTests.Authorization;

public class ScenarioSeedTests : IClassFixture<IamScenario>
{
    private readonly IamScenario _scenario;

    public ScenarioSeedTests(IamScenario scenario) => _scenario = scenario;

    [Fact]
    public void SchemaAndUsersAreCreated()
    {
        Assert.NotEqual(Guid.Empty, _scenario.OwnerUserId);
        Assert.NotEqual(Guid.Empty, _scenario.DirectMemberUserId);
        Assert.NotEqual(Guid.Empty, _scenario.GroupMemberUserId);
        Assert.NotEqual(Guid.Empty, _scenario.OutsiderUserId);
    }

    [Fact]
    public void TwoDistinctAccountsExist()
    {
        Assert.NotEqual(Guid.Empty, _scenario.AccountId);
        Assert.NotEqual(Guid.Empty, _scenario.OtherAccountId);
        Assert.NotEqual(_scenario.AccountId, _scenario.OtherAccountId);
    }

    [Fact]
    public void RolesAndGroupAreCreated()
    {
        Assert.NotEqual(Guid.Empty, _scenario.ReaderRoleId);
        Assert.NotEqual(Guid.Empty, _scenario.EditorRoleId);
        Assert.NotEqual(Guid.Empty, _scenario.UngrantedRoleId);
        Assert.NotEqual(Guid.Empty, _scenario.EditorGroupId);
    }
}
