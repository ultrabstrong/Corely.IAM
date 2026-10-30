# Bearer API

A host that serves an API turns on bearer tokens and, optionally, maps IAM's token endpoints. Its own
endpoints then run with the caller's IAM context, a user's or an API key's, and IAM's services enforce
the caller's permissions. The host writes no token handling.

```csharp
builder.Services.AddIAMWeb(options => options.AcceptBearerTokens = true);

var app = builder.Build();
app.UseIAMWebAuthentication();

app.MapIAMTokenEndpoints("/api/auth");

app.MapGroup("/api")
    .RequireAuthorization()
    .AsIAMApi()
    .MapGet("/roles", async (IUserContextProvider context, IRetrievalService retrieval) =>
        await retrieval.ListRolesAsync(new ListRolesRequest(context.GetUserContext()!.CurrentAccount!.Id)));
```

`AcceptBearerTokens` alone is enough for a host that issues tokens its own way. `MapIAMTokenEndpoints`
refuses to map without it.

An endpoint over the host's own rows still scopes them to the token's account. IAM's check does not
do that for tables it cannot see: see
[Gotchas](../../Corely.IAM/Docs/gotchas.md#permissions-do-not-keep-accounts-apart-in-your-own-tables).

## The token endpoints

| Endpoint | Body | Returns |
|----------|------|---------|
| `POST {prefix}/apikey` | `{ "apiKey": "ciam_...", "deviceId": "billing-sync" }`, device optional | 200 with the pair, or 401 for any refusal |
| `POST {prefix}/renew` | `{ "refreshToken": "..." }` | 200 with a new pair, 409 when another call of the same client renewed first, 401 when the session has ended |
| `POST {prefix}/signout` | none, with the `Authorization` header | 204, or 401 |

The pair is `{ "authToken", "refreshToken", "refreshExpiresUtc" }`. A client sends
`Authorization: Bearer <authToken>`, renews on a 401, keeps the pair the other call received on a 409,
and exchanges the key again when renewal returns 401. A spent refresh token reused after
`RefreshReuseGraceSeconds` ends the whole session.

Users do not sign in through these endpoints; their tokens come from the sign in pages. A user's
token sent as a bearer header is accepted like a key's.

## How a request is read

| The request has | Identity |
|-----------------|----------|
| `Authorization: Bearer` | The token's, and the cookies are ignored. An invalid token leaves the request anonymous |
| No header | The cookies', as before |

`{prefix}/signout` acts only on a bearer token, so a form on another site cannot end a cookie session
through it.

An endpoint marked `AsIAMApi()`, and any request with a bearer header, is answered 401 or 403 with a
`WWW-Authenticate: Bearer` header where a page would redirect to sign in. After an invalid token the
header reads `Bearer error="invalid_token"`.

The principal of an API key carries `ClaimTypes.NameIdentifier` and the `ApiKeyId` claim set to the
key's id, `ClaimTypes.Name` set to its name, and the account claims.

## Rate limiting

IAM ships no limits; the host sets its own with ASP.NET Core's rate limiter. Put `UseRateLimiter`
after `UseIAMWebAuthentication`, so a policy can partition by the caller the token named. A key's
principal carries its id in `ClaimTypes.NameIdentifier`, as a user's does, and a request with no token
falls back to its address, which covers the key exchange.

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("per-caller", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? context.Connection.RemoteIpAddress?.ToString()
                ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 1, Window = TimeSpan.FromSeconds(1) }));
});

app.UseIAMWebAuthentication();
app.UseRateLimiter();

app.MapIAMTokenEndpoints().RequireRateLimiting("per-caller");
app.MapGroup("/api").RequireAuthorization().AsIAMApi().RequireRateLimiting("per-caller");
```

The built in limiters count in memory, per instance. Several instances that share one limit use a
distributed limiter, such as one backed by Redis, through the same `AddPolicy` and
`RequireRateLimiting`. A limit per tier or per key is chosen in the policy, from the claims.

## OpenAPI

The token endpoints carry names, summaries and typed results, so a host's OpenAPI document describes
them with no work. With `Microsoft.AspNetCore.OpenApi`:

```csharp
builder.Services.AddOpenApi();
app.MapOpenApi("/openapi/v1.json");
```

`Corely.IAM.WebApp` serves its document at `/openapi/v1.json`, with the bearer scheme declared and
required on every endpoint that needs authorization; see its `Api/WebAppApiExtensions.cs`.

## Outside HTTP

A host that takes tokens another way, a queue message or a gRPC call, calls
`IAuthenticationService.AuthenticateWithTokenAsync(token)` in the scope that handles it. The key
exchange is `SignInWithApiKeyAsync`. See [API keys](../../Corely.IAM/Docs/authentication.md#api-keys).
