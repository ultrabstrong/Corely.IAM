using Corely.IAM.DataAccessMigrations.Cli.Extensions;
using Corely.IAM.Platform.Models;

namespace Corely.IAM.DataAccessMigrations.Cli.UnitTests;

public class PlatformOwnerCredentialsExtensionsTests
{
    [Fact]
    public void ToSecretsFileText_ListsEveryCredential_ForBootstrappedOwner()
    {
        var accountId = Guid.CreateVersion7();
        var credentials = new PlatformOwnerCredentials(
            accountId,
            "Platform",
            "platform-owner",
            "owner@example.com",
            "s3cret-Password!",
            "JBSWY3DPEHPK3PXP",
            "otpauth://totp/Corely.IAM:owner@example.com?secret=JBSWY3DPEHPK3PXP",
            ["1111-2222", "3333-4444"]
        );

        var text = credentials.ToSecretsFileText(
            new DateTimeOffset(2026, 10, 3, 12, 30, 0, TimeSpan.Zero)
        );

        Assert.Contains("Created (UTC): 2026-10-03 12:30:00", text);
        Assert.Contains($"Account: Platform ({accountId})", text);
        Assert.Contains("Username: platform-owner", text);
        Assert.Contains("Email: owner@example.com", text);
        Assert.Contains("Password: s3cret-Password!", text);
        Assert.Contains("Authenticator secret: JBSWY3DPEHPK3PXP", text);
        Assert.Contains("Authenticator setup URI: otpauth://totp/", text);
        Assert.Contains("  1111-2222", text);
        Assert.Contains("  3333-4444", text);
    }
}
