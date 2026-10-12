using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Audits.Models;
using Corely.IAM.Platform.Entities;
using Corely.IAM.Platform.Mappers;
using Corely.IAM.Platform.Models;
using Corely.IAM.Platform.Providers;
using Microsoft.Extensions.Options;

namespace Corely.IAM.UnitTests.Platform.Providers;

public class SecurityPolicyProviderTests
{
    private readonly ServiceFactory _serviceFactory = new();
    private readonly IRepo<PlatformSecuritySettingsEntity> _repo;
    private readonly ConfiguredSecurityPolicy _configured = new(
        SecurityPolicy.Default with
        {
            MaxLoginAttempts = 7,
        },
        []
    );
    private readonly SecurityPolicyCache _cache = new(
        TimeProvider.System,
        Options.Create(new AuditOptions())
    );
    private readonly SecurityPolicyProvider _provider;

    public SecurityPolicyProviderTests()
    {
        _repo = _serviceFactory.GetRequiredService<IRepo<PlatformSecuritySettingsEntity>>();
        _provider = new SecurityPolicyProvider(_repo, _configured, _cache);
    }

    [Fact]
    public async Task GetPolicy_ReturnsConfiguration_WhenNothingIsSaved() =>
        Assert.Equal(_configured.Policy, await _provider.GetPolicyAsync());

    [Fact]
    public async Task GetPolicy_ReturnsTheSavedSettings_OverConfiguration()
    {
        var saved = SecurityPolicy.Default with { MaxLoginAttempts = 12 };
        await _repo.CreateAsync(saved.ToEntity());

        Assert.Equal(saved, await _provider.GetPolicyAsync());
    }

    [Fact]
    public async Task GetPolicy_ServesTheCache_UntilItIsInvalidated()
    {
        await _provider.GetPolicyAsync();
        var saved = SecurityPolicy.Default with { MaxLoginAttempts = 12 };
        await _repo.CreateAsync(saved.ToEntity());

        Assert.Equal(_configured.Policy, await _provider.GetPolicyAsync());

        _cache.Invalidate();

        Assert.Equal(saved, await _provider.GetPolicyAsync());
    }
}
