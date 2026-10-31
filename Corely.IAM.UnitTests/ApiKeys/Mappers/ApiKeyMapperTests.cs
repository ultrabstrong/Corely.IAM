using Corely.IAM.ApiKeys.Entities;
using Corely.IAM.ApiKeys.Enums;
using Corely.IAM.ApiKeys.Mappers;
using Corely.IAM.ApiKeys.Models;
using Corely.IAM.Groups.Entities;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Entities;
using Corely.IAM.Roles.Entities;

namespace Corely.IAM.UnitTests.ApiKeys.Mappers;

public class ApiKeyMapperTests
{
    [Fact]
    public void CreateApiKeyRequest_ToApiKey_TrimsNameAndBlanksAnEmptyDescription()
    {
        var accountId = Guid.CreateVersion7();

        var apiKey = new CreateApiKeyRequest("  ci  ", accountId, "   ").ToApiKey();

        Assert.Equal("ci", apiKey.Name);
        Assert.Null(apiKey.Description);
        Assert.Equal(accountId, apiKey.AccountId);
    }

    [Fact]
    public void ApiKeyEntity_ToChildRef_CarriesIdAndName()
    {
        var entity = new ApiKeyEntity { Id = Guid.CreateVersion7(), Name = "ci" };

        Assert.Equal(new ChildRef(entity.Id, "ci"), entity.ToChildRef());
    }

    [Fact]
    public void ApiKeyEntity_ToModel_OrdersSecretsBySlot_AndMapsMemberships()
    {
        var entity = new ApiKeyEntity
        {
            Id = Guid.CreateVersion7(),
            Name = "ci",
            Secrets =
            [
                new ApiKeySecretEntity { Slot = ApiKeySlot.Secondary, Prefix = "bbbbbbbb" },
                new ApiKeySecretEntity { Slot = ApiKeySlot.Primary, Prefix = "aaaaaaaa" },
            ],
            Groups = [new GroupEntity { Id = Guid.CreateVersion7(), Name = "Editors" }],
            Roles =
            [
                new RoleEntity
                {
                    Id = Guid.CreateVersion7(),
                    Name = "Owner",
                    IsSystemDefined = true,
                },
            ],
        };

        var model = entity.ToModel();

        Assert.Equal([ApiKeySlot.Primary, ApiKeySlot.Secondary], model.Secrets.Select(s => s.Slot));
        Assert.Equal("Editors", Assert.Single(model.Groups!).Name);
        Assert.True(Assert.Single(model.Roles!).IsSystemDefined);
    }

    [Fact]
    public void ApiKeyEntity_EffectivePermissions_NamesEverySource_ForARoleHeldDirectlyAndThroughAGroup()
    {
        var permission = new PermissionEntity
        {
            Id = Guid.CreateVersion7(),
            ResourceType = "report",
            Read = true,
        };
        var role = new RoleEntity
        {
            Id = Guid.CreateVersion7(),
            Name = "Reader",
            Permissions = [permission],
        };
        var group = new GroupEntity
        {
            Id = Guid.CreateVersion7(),
            Name = "Readers",
            Roles = [role],
        };
        var entity = new ApiKeyEntity
        {
            Name = "ci",
            Roles = [role],
            Groups = [group],
        };

        var effective = Assert.Single(entity.EffectivePermissions()!);

        var source = Assert.Single(effective.Roles);
        Assert.Equal("report", effective.ResourceType);
        Assert.True(source.IsDirect);
        Assert.Equal("Readers", Assert.Single(source.Groups).GroupName);
    }

    [Fact]
    public void ApiKeyEntity_EffectivePermissions_MarksTheRoleIndirect_ForARoleHeldOnlyThroughAGroup()
    {
        var role = new RoleEntity
        {
            Id = Guid.CreateVersion7(),
            Name = "Reader",
            Permissions =
            [
                new PermissionEntity { Id = Guid.CreateVersion7(), ResourceType = "report" },
            ],
        };
        var entity = new ApiKeyEntity
        {
            Name = "ci",
            Roles = [],
            Groups =
            [
                new GroupEntity
                {
                    Id = Guid.CreateVersion7(),
                    Name = "Readers",
                    Roles = [role],
                },
            ],
        };

        var effective = Assert.Single(entity.EffectivePermissions()!);

        Assert.False(Assert.Single(effective.Roles).IsDirect);
    }

    [Fact]
    public void ApiKeyEntity_EffectivePermissions_IsNull_ForAKeyLoadedWithoutItsRoles()
    {
        Assert.Null(new ApiKeyEntity { Name = "ci" }.EffectivePermissions());
    }
}
