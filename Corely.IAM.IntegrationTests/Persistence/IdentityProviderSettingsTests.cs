using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Corely.IAM.IdentityProviders.Entities;
using Corely.IAM.IdentityProviders.Enums;
using Corely.IAM.IdentityProviders.Models;
using Corely.IAM.IdentityProviders.Providers;
using Corely.IAM.IntegrationTests.Infrastructure;
using Corely.IAM.Models;
using Corely.IAM.Platform.Models;
using Corely.IAM.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.IntegrationTests.Persistence;

public sealed class IdentityProviderSettingsTests : IDisposable
{
    private const string SECRET = "configured-google-secret";

    private readonly IamTestHost _host = new(
        configurationOverrides: new Dictionary<string, string?>
        {
            ["SecurityOptions:IdentityProviders:Google:ClientId"] = "google-client",
            ["SecurityOptions:IdentityProviders:Google:ClientSecret"] = SECRET,
            ["SecurityOptions:IdentityProviders:Google:AllowSignUp"] = "false",
        }
    );

    public void Dispose() => _host.Dispose();

    private Task<T> AsSystemAsync<T>(Func<IServiceProvider, Task<T>> work) =>
        _host.WithScopeAsync(services =>
        {
            services.GetRequiredService<IAuthenticationService>().AuthenticateAsSystem("tests");
            return work(services);
        });

    private Task BootstrapAsync() =>
        _host.WithScopeAsync(services =>
            services
                .GetRequiredService<IPlatformService>()
                .BootstrapPlatformAsync(
                    new BootstrapPlatformRequest("Platform", "platform-owner", "owner@example.com")
                )
        );

    private Task<ResolvedIdentityProvider?> UsableAsync(IdentityProviderType type) =>
        _host.WithScopeAsync(services =>
            services.GetRequiredService<IIdentityProviderSettingsProvider>().GetUsableAsync(type)
        );

    private Task<ModifyResult> UpdateAsync(UpdateIdentityProviderRequest request) =>
        AsSystemAsync(services =>
            services.GetRequiredService<IPlatformService>().UpdateIdentityProviderAsync(request)
        );

    [Fact]
    public async Task Configuration_Applies_UntilThereIsAPlatformAccount()
    {
        var google = await UsableAsync(IdentityProviderType.Google);

        Assert.Equal("google-client", google!.Settings.ClientId);
        Assert.Equal(SECRET, google.ClientSecret);
        Assert.False(google.Settings.AllowSignUp);
    }

    [Fact]
    public async Task Bootstrap_CopiesConfiguredProviders_WithTheSecretEncrypted()
    {
        await BootstrapAsync();

        var stored = await _host.QueryAsync(db =>
            db.Set<IdentityProviderEntity>().AsNoTracking().SingleAsync()
        );
        Assert.Equal(IdentityProviderType.Google, stored.Type);
        Assert.False(stored.AllowSignUp);
        Assert.NotNull(stored.EncryptedCredential);
        Assert.DoesNotContain(SECRET, stored.EncryptedCredential);
        Assert.Equal(SECRET, (await UsableAsync(IdentityProviderType.Google))!.ClientSecret);
    }

    [Fact]
    public async Task Update_IsRefused_WithoutAPlatformAccount()
    {
        var result = await UpdateAsync(
            new UpdateIdentityProviderRequest(
                IdentityProviderType.GitHub,
                true,
                true,
                "gh",
                IdentityProviderCredentialKind.ClientSecret,
                clientSecret: "s"
            )
        );

        Assert.Equal(ModifyResultCode.NotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task Update_SavesAProvider_AndNeverReadsItsSecretBack()
    {
        await BootstrapAsync();

        var result = await UpdateAsync(
            new UpdateIdentityProviderRequest(
                IdentityProviderType.GitHub,
                true,
                true,
                "gh-client",
                IdentityProviderCredentialKind.ClientSecret,
                clientSecret: "gh-secret"
            )
        );

        Assert.Equal(ModifyResultCode.Success, result.ResultCode);
        var listed = await AsSystemAsync(services =>
            services.GetRequiredService<IPlatformService>().GetIdentityProvidersAsync()
        );
        var github = listed.Providers!.Single(p => p.Type == IdentityProviderType.GitHub);
        Assert.True(github.HasCredential);
        Assert.Equal(5, listed.Providers!.Count);
        Assert.DoesNotContain("gh-secret", github.ToString());
        Assert.Equal("gh-secret", (await UsableAsync(IdentityProviderType.GitHub))!.ClientSecret);
    }

    [Fact]
    public async Task Update_KeepsTheSecret_WhenNoneIsGiven()
    {
        await BootstrapAsync();

        var result = await UpdateAsync(
            new UpdateIdentityProviderRequest(
                IdentityProviderType.Google,
                false,
                true,
                "google-client",
                IdentityProviderCredentialKind.ClientSecret
            )
        );

        Assert.Equal(ModifyResultCode.Success, result.ResultCode);
        var stored = await _host.QueryAsync(db =>
            db.Set<IdentityProviderEntity>().AsNoTracking().SingleAsync()
        );
        Assert.False(stored.Enabled);
        Assert.NotNull(stored.EncryptedCredential);
    }

    [Fact]
    public async Task Update_RefusesToTurnOnAProviderWithoutItsCredential()
    {
        await BootstrapAsync();

        var result = await UpdateAsync(
            new UpdateIdentityProviderRequest(
                IdentityProviderType.Amazon,
                true,
                true,
                "amzn",
                IdentityProviderCredentialKind.ClientSecret
            )
        );

        Assert.Equal(ModifyResultCode.ValidationError, result.ResultCode);
    }

    [Theory]
    [InlineData(IdentityProviderType.Google, IdentityProviderCredentialKind.Certificate, null)]
    [InlineData(
        IdentityProviderType.GitHub,
        IdentityProviderCredentialKind.ClientSecret,
        "a-tenant"
    )]
    public async Task Update_RefusesSettingsTheProviderDoesNotTake(
        IdentityProviderType type,
        IdentityProviderCredentialKind kind,
        string? tenant
    )
    {
        await BootstrapAsync();

        var result = await UpdateAsync(
            new UpdateIdentityProviderRequest(type, false, true, "client", kind, tenant)
        );

        Assert.Equal(ModifyResultCode.ValidationError, result.ResultCode);
    }

    [Fact]
    public async Task Update_StoresAMicrosoftCertificate_AsItsKeyAndThumbprint()
    {
        await BootstrapAsync();
        using var rsa = RSA.Create(2048);
        using var certificate = new CertificateRequest(
            "CN=iam-test",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1
        ).CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
        var pfx = certificate.Export(X509ContentType.Pfx, "pfx-password");

        var result = await UpdateAsync(
            new UpdateIdentityProviderRequest(
                IdentityProviderType.Microsoft,
                true,
                true,
                "ms-client",
                IdentityProviderCredentialKind.Certificate,
                "organizations",
                certificatePfx: pfx,
                certificatePassword: "pfx-password"
            )
        );

        Assert.Equal(ModifyResultCode.Success, result.ResultCode);
        var microsoft = await UsableAsync(IdentityProviderType.Microsoft);
        Assert.Equal(certificate.Thumbprint, microsoft!.Settings.CertificateThumbprint);
        Assert.Equal("organizations", microsoft.Settings.Tenant);
        using var loaded = microsoft.LoadCertificate();
        Assert.True(loaded.HasPrivateKey);
        Assert.Equal(certificate.Thumbprint, loaded.Thumbprint);
    }

    [Fact]
    public async Task Update_RefusesACertificateThatCannotBeRead()
    {
        await BootstrapAsync();

        var result = await UpdateAsync(
            new UpdateIdentityProviderRequest(
                IdentityProviderType.Microsoft,
                true,
                true,
                "ms-client",
                IdentityProviderCredentialKind.Certificate,
                certificatePfx: [1, 2, 3],
                certificatePassword: "wrong"
            )
        );

        Assert.Equal(ModifyResultCode.ValidationError, result.ResultCode);
    }
}
