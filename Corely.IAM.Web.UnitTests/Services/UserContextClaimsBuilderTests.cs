using System.Security.Claims;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Users.Models;
using Corely.IAM.Web.Security;
using Corely.IAM.Web.Services;

namespace Corely.IAM.Web.UnitTests.Services;

public class UserContextClaimsBuilderTests
{
    private readonly UserContextClaimsBuilder _builder = new();

    private static User CreateUser(Guid? id = null, string? username = null, string? email = null)
    {
        return new User
        {
            Id = id ?? Guid.CreateVersion7(),
            Username = username ?? "testuser",
            Email = email ?? "",
        };
    }

    private static Account CreateAccount(Guid? id = null, string? name = null)
    {
        return new Account
        {
            Id = id ?? Guid.CreateVersion7(),
            AccountName = name ?? "TestAccount",
        };
    }

    private static UserContext CreateUserContext(User? user = null, Account? currentAccount = null)
    {
        return new UserContext(
            user ?? CreateUser(),
            currentAccount,
            "test-device",
            currentAccount != null ? [currentAccount] : []
        );
    }

    [Fact]
    public void BuildPrincipal_NullUserContext_ReturnsUnauthenticatedPrincipal()
    {
        var principal = _builder.BuildPrincipal(null);

        Assert.NotNull(principal);
        Assert.NotNull(principal.Identity);
        Assert.False(principal.Identity.IsAuthenticated);
    }

    [Fact]
    public void BuildPrincipal_UserContextWithNullUser_ReturnsUnauthenticatedPrincipal()
    {
        var context = new UserContext(null!, null, "device", []);

        var principal = _builder.BuildPrincipal(context);

        Assert.NotNull(principal);
        Assert.NotNull(principal.Identity);
        Assert.False(principal.Identity.IsAuthenticated);
    }

    [Fact]
    public void BuildPrincipal_ValidUser_ReturnsAuthenticatedPrincipalWithNameAndIdClaims()
    {
        var userId = Guid.CreateVersion7();
        var user = CreateUser(id: userId, username: "jdoe");
        var context = CreateUserContext(user: user);

        var principal = _builder.BuildPrincipal(context);

        Assert.NotNull(principal.Identity);
        Assert.True(principal.Identity.IsAuthenticated);
        Assert.Equal(userId.ToString(), principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal("jdoe", principal.FindFirstValue(ClaimTypes.Name));
    }

    [Fact]
    public void BuildPrincipal_UserWithEmail_IncludesEmailClaim()
    {
        var user = CreateUser(email: "jdoe@example.com");
        var context = CreateUserContext(user: user);

        var principal = _builder.BuildPrincipal(context);

        Assert.Equal("jdoe@example.com", principal.FindFirstValue(ClaimTypes.Email));
    }

    [Fact]
    public void BuildPrincipal_UserWithoutEmail_DoesNotIncludeEmailClaim()
    {
        var user = CreateUser(email: "");
        var context = CreateUserContext(user: user);

        var principal = _builder.BuildPrincipal(context);

        Assert.Null(principal.FindFirstValue(ClaimTypes.Email));
    }

    [Fact]
    public void BuildPrincipal_WithCurrentAccount_IncludesAccountClaims()
    {
        var accountId = Guid.CreateVersion7();
        var account = CreateAccount(id: accountId, name: "Acme Corp");
        var context = CreateUserContext(currentAccount: account);

        var principal = _builder.BuildPrincipal(context);

        Assert.Equal(
            accountId.ToString(),
            principal.FindFirstValue(AuthenticationConstants.ACCOUNT_ID_CLAIM)
        );
        Assert.Equal(
            "Acme Corp",
            principal.FindFirstValue(AuthenticationConstants.ACCOUNT_NAME_CLAIM)
        );
    }

    [Fact]
    public void BuildPrincipal_WithoutCurrentAccount_DoesNotIncludeAccountClaims()
    {
        var context = CreateUserContext(currentAccount: null);

        var principal = _builder.BuildPrincipal(context);

        Assert.Null(principal.FindFirstValue(AuthenticationConstants.ACCOUNT_ID_CLAIM));
        Assert.Null(principal.FindFirstValue(AuthenticationConstants.ACCOUNT_NAME_CLAIM));
    }
}
