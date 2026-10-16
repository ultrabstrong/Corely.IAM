# Migrating to Corely.IAM.Web 6.0

Built on Corely.IAM 5.0; see [its guide](MIGRATION-5.0.md) for the constants that moved there.

Service and middleware implementations are now internal. Hosts already resolve them through their
interfaces and wire them with `AddIAMWeb`, `AddIAMWebBlazor` and the app extensions, so only code
naming a class directly changes:

| Now internal | Use instead |
|--------------|-------------|
| `AuthCookieManager` | `IAuthCookieManager` |
| `UserContextClaimsBuilder` | `IUserContextClaimsBuilder` |
| `BlazorUserContextAccessor` | `IBlazorUserContextAccessor` |
| `PostAuthenticationFlowService` | `IPostAuthenticationFlowService` |
| `AccountDisplayState` | `IAccountDisplayState` |
| `IamAuthenticationStateProvider` | `AuthenticationStateProvider` |
| `AuthenticationTokenMiddleware`, `CorrelationIdMiddleware` | The app extensions that add them |

`AuthenticationConstants` is gone. Its cookie and claim names are internal.
