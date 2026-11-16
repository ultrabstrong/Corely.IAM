using Corely.IAM.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Corely.IAM.IntegrationTests.Persistence;

public class AccountDeletionCoverageTests : IAsyncLifetime
{
    private const string ACCOUNT_ID = "AccountId";

    private static readonly Dictionary<string, string> Covered = new()
    {
        ["AccountAsymmetricKeyEntity"] = "Cascades from the account",
        ["AccountAuditSettingsEntity"] = "Cascades from the account",
        ["AccountSymmetricKeyEntity"] = "Cascades from the account",
        ["ApiKeyEntity"] = "Cascades from the account",
        ["GroupEntity"] = "Cascades from the account",
        ["InvitationEntity"] = "Cascades from the account",
        ["PermissionEntity"] = "Cascades from the account",
        ["RoleEntity"] = "Cascades from the account",
        ["AuditEntryEntity"] = "No foreign key: PurgeAccountDataAsync deletes it",
        ["AuthTokenEntity"] = "No foreign key: PurgeAccountDataAsync deletes it",
        ["MfaChallengeEntity"] = "No foreign key: PurgeAccountDataAsync deletes it",
        ["EmailVerificationChallengeEntity"] =
            "No foreign key to the account: PurgeAccountDataAsync deletes it",
    };

    private readonly IamScenario _scenario = new();

    public ValueTask InitializeAsync() => _scenario.InitializeAsync();

    public ValueTask DisposeAsync() => _scenario.DisposeAsync();

    [Fact]
    public async Task AccountDeletion_CoversEveryEntityWithAnAccountId_ForTheWholeModel()
    {
        var withAccountId = await _scenario.Host.QueryAsync(db =>
            Task.FromResult(
                db.Model.GetEntityTypes()
                    .Where(t => t.FindProperty(ACCOUNT_ID) is not null)
                    .Select(t => t.ClrType.Name)
                    .ToList()
            )
        );

        var uncovered = withAccountId.Except(Covered.Keys).Order(StringComparer.Ordinal).ToList();

        Assert.True(
            uncovered.Count == 0,
            "Delete these with the account (a cascade, or PurgeAccountDataAsync), then list them here: "
                + string.Join(", ", uncovered)
        );
    }
}
