using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Corely.IAM.ApiKeys.Models;
using Corely.IAM.Audits.Entities;
using Corely.IAM.DataAccess;
using Corely.IAM.IntegrationTests.Infrastructure;
using Corely.IAM.Invitations.Models;
using Corely.IAM.MfaChallenges.Entities;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Entities;
using Corely.IAM.Roles.Models;
using Corely.IAM.Security.Entities;
using Corely.IAM.Services;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.IntegrationTests.Authorization;

public sealed class CrossAccountAttack : IAsyncLifetime
{
    private static readonly JsonSerializerOptions _json = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
    };

    public IamScenario Scenario { get; } =
        new(
            new Dictionary<string, string?>
            {
                ["SecurityOptions:ApiKeysEnabled"] = "true",
                ["SecurityOptions:ApiKeyResourceTypes:0"] = User.RESOURCE_TYPE,
                ["SecurityOptions:ApiKeyResourceTypes:1"] = Role.RESOURCE_TYPE,
                ["SecurityOptions:ApiKeyResourceTypes:2"] = ApiKey.RESOURCE_TYPE,
            }
        );

    public Guid VictimAccountId => Scenario.AccountId;
    public Guid OwnerUserId => Scenario.OwnerUserId;
    public Guid DirectMemberUserId => Scenario.DirectMemberUserId;
    public Guid GroupMemberUserId => Scenario.GroupMemberUserId;
    public Guid ReaderRoleId => Scenario.ReaderRoleId;
    public Guid EditorRoleId => Scenario.EditorRoleId;
    public Guid EditorGroupId => Scenario.EditorGroupId;
    public Guid VictimPermissionId { get; private set; }
    public Guid HeldApiKeyId { get; private set; }
    public Guid BareApiKeyId { get; private set; }
    public Guid VictimInvitationId { get; private set; }
    public Guid VictimAuditEntryId { get; private set; }
    public Guid OwnerSessionId { get; private set; }
    public Guid OwnerTokenId { get; private set; }

    public Guid OutsiderUserId => Scenario.OutsiderUserId;
    public Guid OutsiderAccountId { get; private set; }
    public Guid OutsiderRoleId { get; private set; }
    public Guid OutsiderGroupId { get; private set; }
    public Guid OutsiderPermissionId { get; private set; }
    public Guid OutsiderApiKeyId { get; private set; }

    public async ValueTask InitializeAsync()
    {
        await Scenario.InitializeAsync();

        OutsiderAccountId = await Scenario.ActAsAsync(
            Scenario.OutsiderUsername,
            null,
            async services =>
                (
                    await services
                        .GetRequiredService<IRegistrationService>()
                        .RegisterAccountAsync(
                            new RegisterAccountRequest("Outsider Co", OutsiderUserId)
                        )
                ).CreatedAccountId
        );

        await AsOutsiderAsync(async services =>
        {
            var registration = services.GetRequiredService<IRegistrationService>();
            OutsiderRoleId = (
                await registration.RegisterRoleAsync(
                    new RegisterRoleRequest("Outsider role", OutsiderAccountId)
                )
            ).CreatedRoleId;
            OutsiderGroupId = (
                await registration.RegisterGroupAsync(
                    new RegisterGroupRequest("Outsider group", OutsiderAccountId)
                )
            ).CreatedGroupId;
            OutsiderPermissionId = (
                await registration.RegisterPermissionAsync(
                    new RegisterPermissionRequest(
                        OutsiderAccountId,
                        Role.RESOURCE_TYPE,
                        Guid.Empty,
                        Read: true
                    )
                )
            ).CreatedPermissionId;
            OutsiderApiKeyId = (
                await registration.RegisterApiKeyAsync(
                    new RegisterApiKeyRequest("outsider key", OutsiderAccountId)
                )
            ).CreatedApiKeyId;
            return true;
        });

        await Scenario.ActAsAsync(
            Scenario.OwnerUsername,
            VictimAccountId,
            async services =>
            {
                var registration = services.GetRequiredService<IRegistrationService>();
                HeldApiKeyId = (
                    await registration.RegisterApiKeyAsync(
                        new RegisterApiKeyRequest("held key", VictimAccountId)
                    )
                ).CreatedApiKeyId;
                BareApiKeyId = (
                    await registration.RegisterApiKeyAsync(
                        new RegisterApiKeyRequest("bare key", VictimAccountId)
                    )
                ).CreatedApiKeyId;
                Assert.Equal(
                    AssignRolesToApiKeyResultCode.Success,
                    (
                        await registration.RegisterRolesWithApiKeyAsync(
                            new RegisterRolesWithApiKeyRequest(
                                [ReaderRoleId],
                                HeldApiKeyId,
                                VictimAccountId
                            )
                        )
                    ).ResultCode
                );
                await registration.RegisterApiKeysWithGroupAsync(
                    new RegisterApiKeysWithGroupRequest(
                        [HeldApiKeyId],
                        EditorGroupId,
                        VictimAccountId
                    )
                );
                var invitation = await services
                    .GetRequiredService<IInvitationService>()
                    .CreateInvitationAsync(
                        new CreateInvitationRequest(
                            VictimAccountId,
                            "invitee@example.com",
                            null,
                            3600
                        )
                    );
                Assert.Equal(CreateInvitationResultCode.Success, invitation.ResultCode);
                VictimInvitationId = invitation.InvitationId!.Value;
                return true;
            }
        );

        VictimPermissionId = await Scenario.Host.QueryAsync(db =>
            db.Set<PermissionEntity>()
                .Where(p =>
                    p.AccountId == VictimAccountId
                    && !p.IsSystemDefined
                    && p.Roles!.Any(r => r.Id == ReaderRoleId)
                )
                .Select(p => p.Id)
                .FirstAsync()
        );
        VictimAuditEntryId = await Scenario.Host.QueryAsync(db =>
            db.Set<AuditEntryEntity>()
                .Where(e => e.AccountId == VictimAccountId)
                .Select(e => e.Id)
                .FirstAsync()
        );
        var ownerToken = await Scenario.Host.QueryAsync(db =>
            db.Set<AuthTokenEntity>()
                .Where(t => t.UserId == OwnerUserId && t.RevokedUtc == null)
                .OrderByDescending(t => t.IssuedUtc)
                .FirstAsync()
        );
        OwnerSessionId = ownerToken.SessionId;
        OwnerTokenId = ownerToken.Id;
    }

    public ValueTask DisposeAsync() => Scenario.DisposeAsync();

    public Task<T> AsOutsiderAsync<T>(Func<IServiceProvider, Task<T>> work) =>
        Scenario.ActAsAsync(Scenario.OutsiderUsername, OutsiderAccountId, work);

    public Task WritesNothingAsync(Func<IServiceProvider, Guid, Task> attack) =>
        WritesNothingAsync(attack, [OutsiderAccountId, VictimAccountId]);

    public Task WritesNothingNamingTheVictimAsync(Func<IServiceProvider, Guid, Task> attack) =>
        WritesNothingAsync(attack, [VictimAccountId]);

    public Task WritesNothingAsync(Func<IServiceProvider, Task> attack) =>
        WritesNothingAsync((services, _) => attack(services), [OutsiderAccountId]);

    private async Task WritesNothingAsync(
        Func<IServiceProvider, Guid, Task> attack,
        IReadOnlyList<Guid> requestAccountIds
    )
    {
        var before = await CaptureAsync();

        foreach (var requestAccountId in requestAccountIds)
        {
            await AsOutsiderAsync(async services =>
            {
                await attack(services, requestAccountId);
                return true;
            });
        }

        var after = await CaptureAsync();
        foreach (var (table, rows) in before.Rows)
            Assert.Equal(rows, after.Rows[table]);
        Assert.Empty(before.VictimAuditEntryIds.Except(after.VictimAuditEntryIds));
    }

    public async Task<bool> ChangesAnythingAsync(Func<Task> act)
    {
        var before = await CaptureAsync();
        await act();
        var after = await CaptureAsync();
        return before.Rows.Any(t => !t.Value.SequenceEqual(after.Rows[t.Key]))
            || before.VictimAuditEntryIds.Except(after.VictimAuditEntryIds).Any();
    }

    public async Task<bool> ExposesTheVictimAsync(object? result)
    {
        var serialized = Serialized(result);
        return (await VictimMarkersAsync()).Any(serialized.Contains);
    }

    public async Task LeaksNothingAsync<T>(Func<IServiceProvider, Guid, Task<T>> read)
    {
        foreach (var requestAccountId in new[] { OutsiderAccountId, VictimAccountId })
            await LeaksNothingAsync(services => read(services, requestAccountId));
    }

    public async Task LeaksNothingAsync<T>(Func<IServiceProvider, Task<T>> read)
    {
        var markers = await VictimMarkersAsync();
        var result = await AsOutsiderAsync(read);

        var exposed = markers.Where(Serialized(result).Contains).ToList();

        Assert.Empty(exposed);
    }

    public async Task RefusedAsync<T>(Func<IServiceProvider, Task<T>> call, Func<T, bool> succeeded)
    {
        var result = await AsOutsiderAsync(call);

        Assert.False(succeeded(result));
    }

    private static string Serialized(object? result)
    {
        var node = JsonSerializer.SerializeToNode(
            result,
            result?.GetType() ?? typeof(object),
            _json
        );
        RemoveMessages(node);
        return node?.ToJsonString() ?? string.Empty;
    }

    private static void RemoveMessages(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            obj.Remove("Message");
            foreach (var child in obj.Select(p => p.Value).ToList())
                RemoveMessages(child);
        }
        else if (node is JsonArray array)
        {
            foreach (var child in array)
                RemoveMessages(child);
        }
    }

    private Task<List<string>> VictimMarkersAsync() =>
        Scenario.Host.QueryAsync(async db =>
        {
            Guid[] victimAccounts = [Scenario.AccountId, Scenario.OtherAccountId];
            Guid[] victimUsers = [OwnerUserId, DirectMemberUserId, GroupMemberUserId];
            var ids = new List<Guid>(victimAccounts);
            ids.AddRange(victimUsers);
            ids.AddRange(
                await db.Set<Roles.Entities.RoleEntity>()
                    .Where(r => victimAccounts.Contains(r.AccountId))
                    .Select(r => r.Id)
                    .ToListAsync()
            );
            ids.AddRange(
                await db.Set<Groups.Entities.GroupEntity>()
                    .Where(g => victimAccounts.Contains(g.AccountId))
                    .Select(g => g.Id)
                    .ToListAsync()
            );
            ids.AddRange(
                await db.Set<PermissionEntity>()
                    .Where(p => victimAccounts.Contains(p.AccountId))
                    .Select(p => p.Id)
                    .ToListAsync()
            );
            ids.AddRange(
                await db.Set<ApiKeys.Entities.ApiKeyEntity>()
                    .Where(k => victimAccounts.Contains(k.AccountId))
                    .Select(k => k.Id)
                    .ToListAsync()
            );
            ids.AddRange(
                await db.Set<Invitations.Entities.InvitationEntity>()
                    .Where(i => victimAccounts.Contains(i.AccountId))
                    .Select(i => i.Id)
                    .ToListAsync()
            );
            ids.AddRange(
                await db.Set<AuditEntryEntity>()
                    .Where(e => e.AccountId != null && victimAccounts.Contains(e.AccountId.Value))
                    .Select(e => e.Id)
                    .ToListAsync()
            );
            ids.AddRange(
                await db.Set<AuthTokenEntity>()
                    .Where(t => t.UserId != null && victimUsers.Contains(t.UserId.Value))
                    .Select(t => t.SessionId)
                    .ToListAsync()
            );
            return ids.Select(id => id.ToString()).Distinct().ToList();
        });

    private Task<DatabaseCapture> CaptureAsync() =>
        Scenario.Host.QueryAsync(async db =>
        {
            var rows = new Dictionary<string, List<string>>();
            foreach (var entityType in db.Model.GetEntityTypes())
            {
                var clrType = entityType.ClrType;
                if (
                    clrType == typeof(AuditEntryEntity)
                    || clrType == typeof(MfaChallengeEntity)
                    || clrType.IsAbstract
                    || entityType.IsOwned()
                    || entityType.HasSharedClrType
                )
                    continue;

                var properties = entityType
                    .GetProperties()
                    .Where(p => p.PropertyInfo != null)
                    .Where(p =>
                        !p.Name.Contains("Login", StringComparison.Ordinal)
                        && p.Name != "ModifiedUtc"
                    )
                    .OrderBy(p => p.Name)
                    .ToList();

                var set =
                    (IQueryable<object>)
                        typeof(DbContext)
                            .GetMethod(nameof(DbContext.Set), Type.EmptyTypes)!
                            .MakeGenericMethod(clrType)
                            .Invoke(db, null)!;

                rows[entityType.Name] = set.AsEnumerable()
                    .Where(row => !IsOutsiderToken(row))
                    .Select(row =>
                        string.Join(
                            "|",
                            properties.Select(p => $"{p.Name}={p.PropertyInfo!.GetValue(row)}")
                        )
                    )
                    .Order(StringComparer.Ordinal)
                    .ToList();
            }

            var victimAuditEntryIds = await db.Set<AuditEntryEntity>()
                .Where(e => e.AccountId == VictimAccountId)
                .Select(e => e.Id)
                .ToListAsync();

            return new DatabaseCapture(rows, victimAuditEntryIds);
        });

    private bool IsOutsiderToken(object row) =>
        row is AuthTokenEntity token && token.UserId == OutsiderUserId;

    private sealed record DatabaseCapture(
        Dictionary<string, List<string>> Rows,
        List<Guid> VictimAuditEntryIds
    );
}
