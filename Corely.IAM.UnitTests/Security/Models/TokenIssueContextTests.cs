using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Security.Models;
using Corely.IAM.Users.Constants;
using Corely.IAM.Users.Entities;

namespace Corely.IAM.UnitTests.Security.Models;

public class TokenIssueContextTests
{
    private static readonly DateTime _issuedAt = new(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
    private static readonly DateTime _sessionStarted = _issuedAt.AddDays(-1);

    private static TokenIssueContext Context(List<Account> accounts, Account? signedIn) =>
        new(new UserEntity { Id = Guid.CreateVersion7() }, new(), accounts, signedIn);

    private static List<Claim> Claims(TokenIssueContext context) =>
        context.Claims("jti", _issuedAt, _sessionStarted, "device");

    [Fact]
    public void Claims_CarriesTheIdentityAndTimes()
    {
        var context = Context([], null);

        var claims = Claims(context);

        Assert.Equal(context.UserEntity.Id.ToString(), Value(claims, JwtRegisteredClaimNames.Sub));
        Assert.Equal("jti", Value(claims, JwtRegisteredClaimNames.Jti));
        Assert.Equal("device", Value(claims, UserConstants.DEVICE_ID_CLAIM));
        Assert.Equal(
            new DateTimeOffset(_issuedAt).ToUnixTimeSeconds().ToString(),
            Value(claims, JwtRegisteredClaimNames.Iat)
        );
        Assert.Equal(
            new DateTimeOffset(_sessionStarted).ToUnixTimeSeconds().ToString(),
            Value(claims, UserConstants.SESSION_STARTED_AT_CLAIM)
        );
    }

    [Fact]
    public void Claims_ListsEveryAccount()
    {
        var a = new Account { Id = Guid.CreateVersion7() };
        var b = new Account { Id = Guid.CreateVersion7() };

        var claims = Claims(Context([a, b], null));

        Assert.Equal(
            [a.Id.ToString(), b.Id.ToString()],
            claims.Where(c => c.Type == UserConstants.ACCOUNT_ID_CLAIM).Select(c => c.Value)
        );
    }

    [Fact]
    public void Claims_NamesTheSignedInAccount_OnlyWhenThereIsOne()
    {
        var account = new Account { Id = Guid.CreateVersion7() };

        Assert.Equal(
            account.Id.ToString(),
            Value(Claims(Context([account], account)), UserConstants.SIGNED_IN_ACCOUNT_ID_CLAIM)
        );
        Assert.DoesNotContain(
            Claims(Context([account], null)),
            c => c.Type == UserConstants.SIGNED_IN_ACCOUNT_ID_CLAIM
        );
    }

    private static string Value(List<Claim> claims, string type) =>
        Assert.Single(claims, c => c.Type == type).Value;
}
