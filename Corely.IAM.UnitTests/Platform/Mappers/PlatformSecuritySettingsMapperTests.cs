using System.Reflection;
using Corely.IAM.Platform.Entities;
using Corely.IAM.Platform.Mappers;
using Corely.IAM.Platform.Models;
using Corely.IAM.Security.Models;

namespace Corely.IAM.UnitTests.Platform.Mappers;

public class PlatformSecuritySettingsMapperTests
{
    private static readonly string[] _entityOnlyProperties =
    [
        nameof(PlatformSecuritySettingsEntity.Id),
        nameof(PlatformSecuritySettingsEntity.CreatedUtc),
        nameof(PlatformSecuritySettingsEntity.ModifiedUtc),
    ];

    private static readonly string[] _configurationOnlyProperties =
    [
        nameof(SecurityOptions.PermissionCacheTtlSeconds),
        nameof(SecurityOptions.GoogleClientId),
    ];

    private static readonly SecurityPolicy _policy = new(
        9,
        99,
        false,
        true,
        false,
        true,
        4,
        120,
        600,
        7200,
        3600,
        15,
        120,
        7
    );

    [Fact]
    public void ModelAndEntity_CarryTheSameSettings()
    {
        var modelProperties = typeof(SecurityPolicy)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(p => p.Name)
            .Order();
        var entityProperties = typeof(PlatformSecuritySettingsEntity)
            .GetProperties()
            .Select(p => p.Name)
            .Except(_entityOnlyProperties)
            .Order();

        Assert.Equal(modelProperties, entityProperties);
    }

    [Fact]
    public void ConfigurationCarriesEveryMovedSecurityOptionsSetting()
    {
        var modelProperties = typeof(SecurityPolicy)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(p => p.Name)
            .ToHashSet();
        var optionsProperties = typeof(SecurityOptions)
            .GetProperties()
            .Select(p => p.Name)
            .Except(_configurationOnlyProperties);

        Assert.All(optionsProperties, p => Assert.Contains(p, modelProperties));
    }

    [Fact]
    public void ToEntity_ThenToModel_RoundTrips()
    {
        var entity = _policy.ToEntity();

        Assert.Equal(PlatformSecuritySettingsEntity.SINGLE_ROW_ID, entity.Id);
        Assert.Equal(_policy, entity.ToModel());
    }

    [Fact]
    public void Apply_OverwritesEverySetting()
    {
        var entity = SecurityPolicy.Default.ToEntity();

        entity.Apply(_policy);

        Assert.Equal(_policy, entity.ToModel());
    }
}
