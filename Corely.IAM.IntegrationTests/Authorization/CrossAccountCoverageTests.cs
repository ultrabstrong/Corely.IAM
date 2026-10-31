using Corely.IAM.Roles.Models;
using Corely.IAM.Services;
using Corely.IAM.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.IntegrationTests.Authorization;

public class CrossAccountCoverageTests : IAsyncLifetime
{
    private readonly CrossAccountAttack _attack = new();

    public static TheoryData<string> CaseNames => [.. CrossAccountCases.Cases.Keys.Order()];

    public ValueTask InitializeAsync() => _attack.InitializeAsync();

    public ValueTask DisposeAsync() => _attack.DisposeAsync();

    [Theory]
    [MemberData(nameof(CaseNames))]
    public Task AnOwnerOfAnotherAccount_CannotReachTheVictimsRows(string method) =>
        CrossAccountCases.Cases[method](_attack);

    [Fact]
    public async Task TheWriteCheck_SeesAChangeTheVictimsOwnerMakes()
    {
        var changed = await _attack.ChangesAnythingAsync(() =>
            _attack.Scenario.ActAsAsync(
                _attack.Scenario.OwnerUsername,
                _attack.VictimAccountId,
                services =>
                    services
                        .GetRequiredService<IModificationService>()
                        .ModifyRoleAsync(
                            new UpdateRoleRequest(
                                _attack.ReaderRoleId,
                                _attack.VictimAccountId,
                                "Renamed",
                                null
                            )
                        )
            )
        );

        Assert.True(changed);
    }

    [Fact]
    public async Task TheLeakCheck_SeesTheVictimsOwnReadOfItsRole()
    {
        var read = await _attack.Scenario.ActAsAsync(
            _attack.Scenario.OwnerUsername,
            _attack.VictimAccountId,
            services =>
                services
                    .GetRequiredService<IRetrievalService>()
                    .GetRoleAsync(_attack.ReaderRoleId, true)
        );

        Assert.True(await _attack.ExposesTheVictimAsync(read));
    }

    [Fact]
    public void EveryPublicServiceMethod_HasACrossAccountCase_OrAnExemption()
    {
        var coverage = CrossAccountCoverage.Check(
            typeof(IRegistrationService)
                .Assembly.GetExportedTypes()
                .Where(t =>
                    t.IsInterface
                    && t.Namespace == typeof(IRegistrationService).Namespace
                    && t.Name.EndsWith("Service", StringComparison.Ordinal)
                ),
            CrossAccountCases.Cases.Keys,
            CrossAccountCases.Exemptions
        );

        Assert.True(coverage.IsComplete, coverage.ToString());
    }
}
