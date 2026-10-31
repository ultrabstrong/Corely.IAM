using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Corely.IAM.ApiKeys.Models;
using Corely.IAM.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Services;
using Corely.IAM.Web.FunctionalTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.Web.FunctionalTests.Api;

public class BearerApiTests : FunctionalTestBase
{
    private const string AUTH = "/api/auth";

    protected override IReadOnlyDictionary<string, string> Settings =>
        new Dictionary<string, string>
        {
            ["SecurityOptions:ApiKeysEnabled"] = "true",
            ["SecurityOptions:ApiKeyResourceTypes:0"] = Role.RESOURCE_TYPE,
            ["SecurityOptions:RefreshReuseGraceSeconds"] = "30",
        };

    [Fact]
    public async Task AKey_ExchangesForTokens_AndCallsTheApiAsItself()
    {
        var (apiKeyId, key) = await CreateKeyWithOwnerRoleAsync();

        var pair = await ExchangeAsync(key);
        using var me = await SendAsync(HttpMethod.Get, "/api/me", pair.AuthToken);
        using var roles = await SendAsync(HttpMethod.Get, "/api/roles", pair.AuthToken);

        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        var caller = (await ReadJsonAsync(me)).GetProperty("data");
        Assert.Equal("api_key", caller.GetProperty("principalType").GetString());
        Assert.Equal(apiKeyId, caller.GetProperty("principalId").GetGuid());
        Assert.Equal(AccountId, caller.GetProperty("accountId").GetGuid());

        Assert.Equal(HttpStatusCode.OK, roles.StatusCode);
        var names = (await ReadJsonAsync(roles))
            .GetProperty("data")
            .EnumerateArray()
            .Select(r => r.GetProperty("name").GetString());
        Assert.Contains(Role.OWNER_ROLE_NAME, names);
    }

    [Fact]
    public async Task AWrongKey_IsRefusedWith401()
    {
        await CreateKeyWithOwnerRoleAsync();

        using var response = await PostJsonAsync(
            $"{AUTH}/apikey",
            new { apiKey = "ciam_abcdefgh_wrongsecret" }
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertFailureAsync(response, "Unauthorized");
    }

    [Fact]
    public async Task AnApiCallWithNoToken_Gets401_NotARedirect()
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/me", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Bearer", response.Headers.WwwAuthenticate.Single().Scheme);
        await AssertFailureAsync(response, "Unauthorized");
    }

    [Fact]
    public async Task AnUnknownApiRoute_GetsTheFailureShape()
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/nothing-here", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertFailureAsync(response, "NotFound");
    }

    [Fact]
    public async Task AMalformedBody_GetsTheFailureShape()
    {
        using var client = Factory.CreateTestClient();
        using var response = await client.PostAsync(
            $"{AUTH}/apikey",
            new StringContent("{not json", System.Text.Encoding.UTF8, "application/json")
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertFailureAsync(response, "BadRequest");
    }

    [Fact]
    public async Task APageOutsideTheApi_IsNotGivenTheFailureShape()
    {
        using var response = await SendAsync(HttpMethod.Get, "/not-an-api-page", null);

        Assert.NotEqual("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task AnInvalidToken_Gets401_NamingTheTokenInvalid()
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/me", "not-a-jwt");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(
            "error=\"invalid_token\"",
            response.Headers.WwwAuthenticate.Single().Parameter
        );
    }

    [Fact]
    public async Task Renewal_ReturnsANewPair_409sACloseReplay_AndEndsTheSessionOnALateOne()
    {
        var (_, key) = await CreateKeyWithOwnerRoleAsync();
        var first = await ExchangeAsync(key);

        using var renewed = await PostJsonAsync(
            $"{AUTH}/renew",
            new { refreshToken = first.RefreshToken }
        );
        Assert.Equal(HttpStatusCode.OK, renewed.StatusCode);
        var second = (await renewed.Content.ReadFromJsonAsync<Envelope<TokenPair>>(Json))!.Data;

        using (var me = await SendAsync(HttpMethod.Get, "/api/me", second.AuthToken))
            Assert.Equal(HttpStatusCode.OK, me.StatusCode);

        using (
            var replay = await PostJsonAsync(
                $"{AUTH}/renew",
                new { refreshToken = first.RefreshToken }
            )
        )
        {
            Assert.Equal(HttpStatusCode.Conflict, replay.StatusCode);
            await AssertFailureAsync(replay, "AlreadyRenewed");
        }

        Clock.Advance(TimeSpan.FromSeconds(31));
        using (
            var lateReplay = await PostJsonAsync(
                $"{AUTH}/renew",
                new { refreshToken = first.RefreshToken }
            )
        )
            Assert.Equal(HttpStatusCode.Unauthorized, lateReplay.StatusCode);

        using var afterReuse = await SendAsync(HttpMethod.Get, "/api/me", second.AuthToken);
        Assert.Equal(HttpStatusCode.Unauthorized, afterReuse.StatusCode);
    }

    [Fact]
    public async Task SignOut_EndsTheBearerSession()
    {
        var (_, key) = await CreateKeyWithOwnerRoleAsync();
        var pair = await ExchangeAsync(key);

        using var signOut = await SendAsync(HttpMethod.Post, $"{AUTH}/signout", pair.AuthToken);
        using var me = await SendAsync(HttpMethod.Get, "/api/me", pair.AuthToken);
        using var renew = await PostJsonAsync(
            $"{AUTH}/renew",
            new { refreshToken = pair.RefreshToken }
        );

        Assert.Equal(HttpStatusCode.OK, signOut.StatusCode);
        Assert.Equal(
            "Success",
            (await ReadJsonAsync(signOut)).GetProperty("resultCode").GetString()
        );
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, renew.StatusCode);
    }

    [Fact]
    public async Task SignOut_RefusesACookieSession_SoAnotherSiteCannotEndIt()
    {
        await SignInSuccessfullyAsync();

        using var signOut = await Client.SendAsync(
            new HttpRequestMessage(HttpMethod.Post, $"{AUTH}/signout")
        );
        using var me = await Client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, signOut.StatusCode);
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
    }

    [Fact]
    public async Task ABearerHeader_WinsOverTheCookies()
    {
        await SignInSuccessfullyAsync();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-jwt");
        using var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TheOpenApiDocument_DescribesTheTokenEndpoints_AndTheBearerScheme()
    {
        using var response = await SendAsync(HttpMethod.Get, "/openapi/v1.json", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var document = await ReadJsonAsync(response);
        var paths = document.GetProperty("paths");
        Assert.True(paths.TryGetProperty($"{AUTH}/apikey", out _));
        Assert.True(paths.TryGetProperty($"{AUTH}/renew", out _));
        Assert.True(paths.TryGetProperty($"{AUTH}/signout", out _));
        Assert.True(paths.TryGetProperty("/api/roles", out _));
        Assert.True(
            document
                .GetProperty("components")
                .GetProperty("securitySchemes")
                .TryGetProperty("Bearer", out _)
        );
    }

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private sealed record Envelope<T>(string ResultCode, string? Message, T Data);

    private sealed record TokenPair(
        string AuthToken,
        string RefreshToken,
        DateTime RefreshExpiresUtc
    );

    private async Task<(Guid ApiKeyId, string Key)> CreateKeyWithOwnerRoleAsync() =>
        await Factory.WithScopeAsync(async services =>
        {
            var signIn = await services
                .GetRequiredService<IAuthenticationService>()
                .SignInAsync(
                    new SignInRequest(
                        SeedData.OwnerUsername,
                        SeedData.OwnerPassword,
                        "seed-device",
                        AccountId
                    )
                );
            Assert.Equal(SignInResultCode.Success, signIn.ResultCode);

            var roles = await services
                .GetRequiredService<IRetrievalService>()
                .ListRolesAsync(new ListRolesRequest(AccountId));
            var ownerRoleId = roles.Data!.Items.Single(r => r.Name == Role.OWNER_ROLE_NAME).Id;

            var registration = services.GetRequiredService<IRegistrationService>();
            var created = await registration.RegisterApiKeyAsync(
                new RegisterApiKeyRequest("ci", AccountId)
            );
            Assert.Equal(CreateApiKeyResultCode.Success, created.ResultCode);

            var assigned = await registration.RegisterRolesWithApiKeyAsync(
                new RegisterRolesWithApiKeyRequest(
                    [ownerRoleId],
                    created.CreatedApiKeyId,
                    AccountId
                )
            );
            Assert.Equal(AssignRolesToApiKeyResultCode.Success, assigned.ResultCode);

            return (created.CreatedApiKeyId, created.Key!);
        });

    private async Task<TokenPair> ExchangeAsync(string key)
    {
        using var response = await PostJsonAsync($"{AUTH}/apikey", new { apiKey = key });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<Envelope<TokenPair>>(Json))!.Data;
    }

    private async Task<HttpResponseMessage> PostJsonAsync(string path, object body)
    {
        using var client = Factory.CreateTestClient();
        return await client.PostAsJsonAsync(path, body);
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string path,
        string? bearerToken
    )
    {
        using var client = Factory.CreateTestClient();
        using var request = new HttpRequestMessage(method, path);
        if (bearerToken is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        return await client.SendAsync(request);
    }

    private static async Task AssertFailureAsync(HttpResponseMessage response, string resultCode)
    {
        var body = await ReadJsonAsync(response);

        Assert.Equal(resultCode, body.GetProperty("resultCode").GetString());
        Assert.False(string.IsNullOrEmpty(body.GetProperty("message").GetString()));
        Assert.Equal(JsonValueKind.Null, body.GetProperty("data").ValueKind);
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(Json);
}
