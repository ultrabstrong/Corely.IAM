using System.Net;
using System.Text.Json;
using Corely.IAM.Security.Models;
using Corely.IAM.Web.FunctionalTests.Infrastructure;
using Microsoft.IdentityModel.Tokens;

namespace Corely.IAM.Web.FunctionalTests.Api;

public class JwksEndpointTests : FunctionalTestBase
{
    private string JwksPath(Guid accountId) => $"/api/accounts/{accountId}/.well-known/jwks.json";

    [Fact]
    public async Task Jwks_ServesTheRawKeySet_WithNoSignIn()
    {
        using var response = await Client.GetAsync(JwksPath(AccountId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/jwk-set+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("public, max-age=300", response.Headers.CacheControl?.ToString());

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var json = JsonDocument.Parse(body);
        Assert.False(json.RootElement.TryGetProperty("resultCode", out _));

        var keySet = new JsonWebKeySet(body);
        var signing = Assert.Single(keySet.Keys, k => k.Use == "sig");
        Assert.Equal("EC", signing.Kty);
        Assert.Equal(SecurityAlgorithms.EcdsaSha256, signing.Alg);
        Assert.Equal(
            AccountJsonWebKey.KeyIdFor(AccountId, AccountJsonWebKey.USE_SIGNATURE, 1),
            signing.Kid
        );
        var encryption = Assert.Single(keySet.Keys, k => k.Use == "enc");
        Assert.Equal("RSA", encryption.Kty);
        Assert.Equal("RSA-OAEP-256", encryption.Alg);
    }

    [Fact]
    public async Task Jwks_AnswersAPlainEmpty404_ForAMissingAccount()
    {
        using var response = await Client.GetAsync(JwksPath(Guid.CreateVersion7()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task OpenApi_ListsTheKeySet_AndNothingThatRevealsAKey()
    {
        using var response = await Client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var document = await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken
        );
        Assert.Contains("jwks.json", document);
        Assert.DoesNotContain("Reveal", document, StringComparison.OrdinalIgnoreCase);
    }
}
