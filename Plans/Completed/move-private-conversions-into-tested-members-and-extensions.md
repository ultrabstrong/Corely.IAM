# Move private conversions and readings into tested members and extensions, in IAM, Common, DataAccess and Security

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
3. **Convert every `this T` extension** in the repository to an `extension(T)` block, public ones
   included, not only files a move touches. Re-count first: the census above came from a
   single-line search and misses signatures that wrap. A C# 14 extension block compiles to the same
   static method, so public extensions in Common and DataAccess stay binary-compatible; prove it by
   building IAM against locally built Common and DataAccess, without publishing anything.
4. **Update `CLAUDE.md`.** Once a repository has no `this T` left, drop "(existing ones convert
   when next touched)" from its seams rule, so it simply says new code uses `extension(T)`.
5. **Push.** When `RebuildAndTest.ps1` is green, push the repository's `master`. Do not publish
   packages and do not bump versions: this is code organisation, not a change to how the packages
   are used. A move that stays internal needs only a patch bump when the package next ships;
   anything that becomes public API, as `TimeBucketExtensions` did in Billing, is a minor bump and
   gets a line in the docs at that point.

## Audit

### Corely.Common

| Where | Method | Verdict |
|---|---|---|
| `HttpRequestResponseLoggingHandler` | `OmitJsonFields`, `TruncateJsonFields` | Extension on `string`: `WithJsonFieldsOmitted`, `WithJsonFieldTruncated` in `StringExtensions` |
| `HttpRequestResponseLoggingHandler` | `BuildHeadersSnapshot` | Extension on `HttpHeaders`: `ToLoggingSnapshot` in new internal `HttpHeadersExtensions`; `MaskIfSensitive` stays private there as a step of it |
| `HttpRequestResponseLoggingHandler` | `LogRequestAsync`, `LogResponseAsync` | Stays: structures `SendAsync` |
| `FilePathProvider` | `GetOverwriteProtectedFileName`, `RemoveLastExtensionOccurrence` | Extension on `FileInfo`: `NumberedName`, `NameWithoutExtension` in new internal `FileInfoExtensions` |
| `ByteArrayBomExtensions` | `IsMatch` | Stays: a private step inside the extension class |
| `ComparableFilter`, `EnumFilter`, `GuidFilter`, `StringFilter` | `Build…Expression`, `Constant` | Stays: each builds the filter's own expression from its own state |
| `FilterBuilder` | `ValidateMemberAccess`, `ParameterReplacer` | Stays: structures `Where`/`OrderBy` building |
| `ExpressionMapper` | `PropertyRemappingVisitor` | Stays: the mapper's own visitor |
| `DelimitedTextProvider` | `ReadNextRecord`, `WriteRecord`, `AppendTokenLiteral` | Stays: the provider's parse and write loop |
| `PagedResult` | `UpdatePage` | Stays: structures its own paging |
| `PasswordRedactionProvider` | generated `Regex` partials | Stays |

Every `this T` converted; public signatures verified identical against pushed `master` by
reflection. Mutation checks for the new tests were not run: auto mode blocked running tests against
deliberately broken redaction code, so that step waits for the owner.

### Corely.DataAccess

| Where | Method | Verdict |
|---|---|---|
| `EFEventDataLogger` | `GetEffectiveLevel` | Extension on `LogLevel`: `WithInformationWrittenAs` in new internal `LogLevelExtensions` |
| `EFEventDataLogger` | `BuildParameterDictionary` | Extension on `DbParameterCollection`: `ToLoggingDictionary` in new internal `DbParameterCollectionExtensions` |
| `EFEventDataLogger` | `LogCommandExecuted`, `LogBasicEvent` | Stays: structures `Write` |
| `MockUpdateSetters` | `ResolveProperty` | Extension on `Expression<Func<TEntity, TProperty>>`: `SelectedProperty` in new internal `ExpressionExtensions` |
| `MockRepo` | `TryGetId`, `GetIdOrNull`, `IsCreatedUtcUnset`, `EnsureCreatedUtc` | Stays: the receiver is `object`, and an extension on `object` would surface on every type; the typed receiver, `IHasGeneratedIdPk<>`, is open generic. They are the mock's own emulation of EF identity and are covered through `MockRepo`'s public API |
| `EFContextResolver` | `ResolveContextType`, `DiscoverRegisteredContextTypes` | Stays: structures the resolver's cache |

`this T` converted everywhere except `EntityTypeBuilderExtensions.ConfigureIdPk<TEntity, TKey>`:
`TKey` cannot come from the receiver, so a block member would change every call site from
`ConfigureIdPk<TEntity, TKey>()` to `ConfigureIdPk<TKey>()`. `CLAUDE.md` records the exception.
Public static signatures verified identical against pushed `master` by reflection.

### Corely.Security

| Where | Method | Verdict |
|---|---|---|
| `FileSymmetricKeyStoreProvider` | `TrimWhitespace`, `IsWhitespace` | Extension on `byte[]`: `WithoutSurroundingWhitespace` in new internal `KeyStore/ByteArrayExtensions`; `IsWhitespace` stays private there |
| `FileAsymmetricKeyStoreProvider` | `SplitLines` | Extension on `byte[]`: `NonEmptyLineRanges` |
| `FileAsymmetricKeyStoreProvider`, `FileSymmetricKeyStoreProvider` | `Decode`, and the same code inline | Extension on `ReadOnlySpan<byte>`: `DecodedBase64Key`, shared by both |
| `RsaEncryptionProvider` | `PaddingName` | Extension on `RSAEncryptionPadding`: `ShortName` |
| `SaltedHashProviderBase`, `Pbkdf2HashProvider` | `FormatHashedValue`, `TryParse`, `CreateSalt`, `CreateSaltedValue`, `Derive` | Stays: the provider's own hash format and derivation, bound to its provider code and covered by the public hash/verify round trip |
| `SymmetricEncryptionProviderBase`, `AsymmetricEncryptionProviderBase` | `FormatEncryptedValue`, `NamesADifferentProvider` | Stays: same reason, the provider's own encrypted-value format |
| The five provider factories | `Validate` | Stays: structures `AddProvider` |
| `PasswordValidationProvider` | `CreateRegex`, `DetailedValidation` | Stays: structures `ValidatePassword` |
| `SymmetricEncryptionRewriter`, `AsymmetricEncryptionRewriter` | `VerifyReadsBack` | Stays: structures the rewrite |
| `Corely.Security.DemoApp/Program.cs` | 11 demo steps | Skipped: demo |

The one `this T` (a test-file helper) converted.

### Corely.IAM

| Where | Method | Verdict |
|---|---|---|
| `AuthenticationProvider` | `GetSignatureKey`, `GetAccountModels` | `UserEntity` readings in `UserMapper`: `SignatureKey`, `AccountModels` |
| `AuthenticationProvider` | `GetClaimValue`, `GetSessionStartedUtc`, the claim parse in `ExtractSignedInAccountFromToken` | Extension on `JwtSecurityToken`: `ClaimValue`, `SessionStartedUtc`, `SignedInAccountId` |
| `AuthenticationProvider` | `BuildTokenClaims` | Member of `TokenIssueContext`, now its own internal record: `Claims` |
| `AuthenticationProvider` | `CreateFailedTokenResult`, `CreateFailedRenewTokenResult`, `CreateFailedValidationResult` | Internal `Failed(code)` on each result record |
| `AuthenticationProvider` | `FindAccountById`, `IsWithinLifetime`, `ValidateJwtToken`, `CreateUserAuthTokenAsync`, `GetUserWithKeysAndAccountsAsync`, `RevokeExistingTokensForUserAccountDeviceAsync`, rest of `ExtractSignedInAccountFromToken` | Stays: structures token issue and validation |
| `AuthenticationService` | `MapAuthTokenResultCode` + `CreateFailedSignInResult`, `MapRenewAuthTokenResultCode` + `CreateFailedRenewAuthTokenResult` | Extensions on the provider codes: `ToFailedSignInResult`, `ToFailedRenewAuthTokenResult`; internal `Failed(code, message)` on `SignInResult`, `RenewAuthTokenResult` |
| `AuthenticationService` | `CreateMfaChallengeAsync`, `GenerateAuthTokenAndSetContextAsync`, `GetUserSessionContext` | Stays: structures sign-in |
| `AuthorizationProvider` | `HasAction` | `PermissionEntity.Allows` in `PermissionMapper` |
| `AuthorizationProvider` | `TryGetUserContext`, `IsCacheValidFor`, `GetPermissionsAsync`, `TryGetUserId` | Stays: the provider's cache and context |
| `RoleProcessor` | `IsOwnerSystemPermission` | `PermissionEntity.IsOwnerSystemPermission` in `PermissionMapper` |
| `TotpProvider` | `Base32Encode`, `Base32Decode` | `byte[].ToBase32`, `string.FromBase32` in `Corely.IAM/Extensions` |
| `TotpProvider` | `ComputeCode`, `GetCurrentTimeStep` | Stays: the provider's HOTP step, covered through `GenerateCode`/`ValidateCode` with a fake clock |
| `TotpAuthProcessor` | `FormatRecoveryCode` | `string.ToDisplayRecoveryCode` |
| `TotpAuthProcessor` | `EncryptWithSystemKey`, `DecryptWithSystemKey`, `GenerateRecoveryCodesAsync`, `GenerateRecoveryCode` | Stays: wraps injected services and randomness |
| `PasswordRecoveryProcessor` | `CreateToken`, `TryParseToken` | New internal record `PasswordRecoveryToken` with `ToString` and `TryParse` |
| `PasswordRecoveryProcessor` | `InvalidatePendingRecoveriesAsync`, `SetPasswordAsync`, `ValidateRecoveryTokenAsync` | Stays: structures recovery |
| `RegistrationService` | `GenerateUsernameFromEmail` | `string.EmailLocalPart` |
| `RegistrationService` | `GenerateRandomSuffix` | Stays: randomness |
| `RetrievalService` | `WrapListResultAsync` | `ListResult<T>.ToRetrieveListResult` |
| `RetrievalService` | `GetEffectivePermissionsAsync`, `GetCurrentAccountId` | Stays: reads the injected context |
| `InvitationProcessor`, `BasicAuthProcessor`, `GoogleIdTokenValidator` | `GetRequiredUserContext`, `UpgradeStoredHashIfNeededAsync`, `GetConfigurationManager` | Stays: structures the class |
| `DataAccessMigrations.Cli/DbCommandBase` | `PlaceholderConnectionString` | Extension on `DatabaseProvider` |
| `DataAccessMigrations.Cli` | `FirstNonBlank`, `Report`, `CommandBase` reflection helpers | Stays: structures command building |
| `Web/SecurityHeadersMiddleware` | `IsStaticAssetRequest` | `PathString.IsCacheableStaticAsset`. Extension matching is case-sensitive, so `/img/logo.PNG` is not cached; kept as found and recorded in a test |
| `Web/PermissionView` | `ResourceIdsEqual` | `Guid[]?.SameIdsAs` |
| `Web` middleware, pages and Razor components | event handlers, loaders, `RunSafeAsync`, `SetMessage`, sort icons | Stays: UI state and handlers |

`Corely.IAM.Web` gained `InternalsVisibleTo` for its unit tests. Every `this T` converted, including
the Web registration classes and the tool service factories; `LoggerExtensions` keeps its method type
parameters on the members. Public static signatures of `Corely.IAM` and `Corely.IAM.Web` verified
identical against pushed `master` by reflection.

## Decisions (owner)

1. **Sweep all `this T` extensions now**, in all four repositories, public ones included. Skip one
   only for a really good reason, and record the reason here.
2. **Order:** Common, DataAccess and Security first, because they are small and IAM depends on them,
   then IAM.
3. **Push each repository** when it is green. No package publishing and no version bumps.

## Outcome

Done in all four repositories, each pushed with `RebuildAndTest.ps1` green; no packages published and
no versions bumped. Every census row has a verdict above, every move has direct tests, and the only
`this T` left is `ConfigureIdPk` in DataAccess, for the reason recorded there.

The break-and-restore check (Work step 2) was run afterwards, on the owner's say-so: one deliberate
bug per move, the move's own tests run, the file restored. 58 of 60 across these repositories,
Billing and DocsToData went red first time. The two that did not were test gaps, now closed:
`NameWithoutExtension` had no name whose extension text repeats, and `IsCacheableStaticAsset` had no
`/_content` path without a cacheable extension.

## Done when

Every census row has a verdict here, each move has direct tests, no `this T` extensions remain (or
each survivor has a recorded reason), every repository's `RebuildAndTest.ps1` is green and pushed,
and this plan moves to `Plans/Completed/` with an Outcome section.
