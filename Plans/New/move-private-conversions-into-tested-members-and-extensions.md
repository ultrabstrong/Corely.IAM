# Apply the seam rule for readings and conversions across the Corely repositories

## Starting cold

The rule is **Seams for readings and conversions** in each repository's `CLAUDE.md` (Corely.IAM,
Corely.Common, Corely.DataAccess, Corely.Security, Corely.Billing). In short: a reading or
conversion of a type is a contract someone can get wrong, so it cannot hide in a private helper
reachable only through the class that calls it. It goes:

- on the type itself, when we own the type and the reading belongs to its layer;
- in a C# 14 `extension(T)` block, when the type is an enum, BCL or vendor type, or the reading
  belongs to another layer;
- in the domain's `Mappers` for entities, which stay plain data.

Each move gets its own direct unit tests. A private helper that only structures the method it sits
in stays where it is.

**Reference application:** Corely.Billing, commit `7ddeb09` plus the follow-up that put
`QuotaContext.RemainingRatio` and `RetryOptions.RetryDelay` on their records. It shows every kind
of verdict, the tests that came with each move, and the helpers that were deliberately left private.
Read its commit message for the list.

## Census

Private methods in production code, excluding tests, demos, dev tools and migrations:

| Repository | Private methods | Files | Biggest |
|------------|-----------------|-------|---------|
| Corely.IAM | 59 | 18 | `AuthenticationProvider` (16), `AuthenticationService` (5), `AuthorizationProvider` (5), `TotpAuthProcessor` (5), `TotpProvider` (4), `PasswordRecoveryProcessor` (4), `ListQueryHelper` (3), `RetrievalService` (3), `AuthenticationTokenMiddleware` (3) |
| Corely.Security | 33 | 16 | `Corely.Security.DemoApp/Program.cs` (11, demo; skip), `SaltedHashProviderBase` (4), `Pbkdf2HashProvider` (2) |
| Corely.Common | 26 | 12 | `HttpRequestResponseLoggingHandler` (6), `ComparableFilter` (4), `DelimitedTextProvider` (3), `FilterBuilder` (3) |
| Corely.DataAccess | 12 | 5 | `MockRepo` (4), `EFEventDataLogger` (4), `EFContextResolver` (2) |

Most will turn out to be structuring helpers that stay. The count is where to look, not the
amount of work.

`this T` extensions still in the old form, 42 across 22 files: IAM's domain mappers (`AccountMapper`,
`UserMapper`, `RoleMapper`, `GroupMapper`, `PermissionMapper`, `InvitationMapper`, `BasicAuthMapper`,
the three `Security/Mappers`, `ValidationMapper`) and its two Web registration classes; Common's
`StringExtensions`, `RegexExtensions`, `ByteArrayBomExtensions` and the two `Http…LoggingExtensions`;
DataAccess's `ServiceRegistrationExtensions` and `AsyncQueryableExtensions`.

## Work, per repository

One repository at a time, one commit per moved type, tests alongside.

1. **Audit.** For every private method in the census, record a verdict in a table in this plan:
   *member of `<Type>`*, *extension on `<Type>` in `<layer>`*, *mapper*, or *stays: structures
   `<method>`*. Watch for grab-bag helper classes not attached to a type as well as private
   methods; Billing had one (`BillingMessages`).
2. **Move and test.** Name each for what it returns. Prove each new test catches the bug it guards:
   break the moved code, watch the test fail, restore.
3. **Convert `this T` in any file the move touches** to an `extension(T)` block. A C# 14 extension
   block compiles to the same static method, so public extensions in Common and DataAccess stay
   binary-compatible; confirm that with a consumer build before releasing.
4. **Version.** A move that stays internal needs only a patch bump when the package next ships.
   Anything that becomes public API, as `TimeBucketExtensions` did in Billing, is a minor bump and
   gets a line in the docs.

## Open questions for the owner

1. **Sweep the remaining `this T` extensions now, or when next touched?** The rule says when next
   touched. The IAM mappers are mechanical and number 14; doing them in one pass keeps the
   repository consistent. Recommend: sweep IAM's mappers in this work, leave Common's and
   DataAccess's public ones until those files change for another reason.
2. **Order.** Recommend Common, DataAccess and Security first, because they are small and IAM
   depends on them, then IAM.

## Done when

Every census row has a verdict here, each move has direct tests, every repository's
`RebuildAndTest.ps1` is green, and this plan moves to `Plans/Completed/` with an Outcome section.
