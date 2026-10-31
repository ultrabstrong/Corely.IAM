# Migrating to Corely.IAM.Web 9.0

Every API response now has one shape, and the token endpoints use it.

```json
{ "resultCode": "Success", "message": null, "data": { "authToken": "..." } }
```

```json
{ "resultCode": "Unauthorized", "message": "The key was refused.", "data": null }
```

## What breaks

| Endpoint | Before | After |
|----------|--------|-------|
| `POST {prefix}/apikey` | The pair at the top level; 401 with no body | The pair under `data`; 401 with the shape |
| `POST {prefix}/renew` | The pair at the top level; 401 and 409 with no body | The pair under `data`; 401 and 409 with the shape. A 409's `resultCode` is `AlreadyRenewed` |
| `POST {prefix}/signout` | 204 | 200 with the shape and no `data` |

A client reads the pair from `data` and otherwise behaves as before: the status codes that mean renew
or sign in again have not moved, apart from sign out's success.

## What to add

Give the host's own API the same shape on every failure, including those no endpoint wrote:

```csharp
app.UseIAMApiResponses("/api");
app.UseIAMWebAuthentication();
```

Put it before anything that can answer a request under that prefix. It covers an empty 400, 401, 403,
404, 413 and 429, and an unhandled exception, whose details never reach the caller. An endpoint
answers with `ApiResponse.Success(data)`, or `ApiResponse.Failure(code, message)` with its status code.

## What is new

- The API key page has an Access section: what the platform lets keys use, what this key holds, which
  role or group each came from, and anything its roles grant that the platform does not allow.
  `Corely.IAM` 6.2.0 fills `ApiKey.EffectivePermissions` when a key is read hydrated.
- `EffectivePermissionGrants` is the permission and source list from `EffectivePermissionsPanel`, on
  its own.
