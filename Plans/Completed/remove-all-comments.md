# Remove All Comments

## Starting cold

For a session picking this up with no history. The change spans six repositories, each done the
same way:

| Repository | Path |
|---|---|
| Corely.Common | `C:\source\git\ultrabstrong\Corely.Common` |
| Corely.Security | `C:\source\git\ultrabstrong\Corely.Security` |
| Corely.DataAccess | `C:\source\git\ultrabstrong\Corely.DataAccess` |
| Corely.IAM | `C:\source\git\ultrabstrong\Corely.IAM` |
| Corely.Billing | `C:\source\git\ultrabstrong\Corely.Billing` |
| DocsToData | `C:\source\git\pinnacleinnovation\DocsToData` |

Read each repository's `CLAUDE.md` before touching it. Commit locally per repository; push only when
the owner says so.

**Decided with the owner; do not reopen:** the repository list; the file extensions; the rule lives
in a path-scoped `.claude/rules/` file, not in `CLAUDE.md`; no CI check; approved comments are not
recorded anywhere, since a comment existing is the proof it was approved.

## Why

The comments in these repositories are read by one audience: whoever edits the code next. In
practice that is the owner and Claude sessions. Nobody else sees them:

- **No package ships its XML docs.** No project sets `GenerateDocumentationFile`, so the `///`
  blocks never reach a consumer's IntelliSense. What consumers read is `Docs/`, which the XML docs
  mostly repeat.
- **Every comment costs tokens on every read.** A session reads the comment and the code it
  describes. When the comment restates the code, the same fact is paid for twice, every time.
- **Every comment is a maintenance item nothing checks.** Change the behavior, miss the comment, and
  it is now wrong. A later session trusts the prose over the code.
- **The current rule does not hold.** Four repositories say "explain why, not what". That opens the
  door to paragraph-long rationale above every branch, which is most of what is there now. Much of it
  is a session writing down its reasoning, which belongs in a plan or a commit message.

## Scale

Comment lines in `.cs`, `.razor` and `.cshtml`, generated migrations excluded, counted when this
plan was written:

| Repository | Comment lines | Files with any | Files | Kept (below) |
|---|---:|---:|---:|---:|
| Corely.Common | 172 | 16 | 74 | 0 |
| Corely.Security | 286 | 38 | 128 | 10 |
| Corely.DataAccess | 240 | 47 | 95 | 4 |
| Corely.IAM | 908 | 129 | 843 | 33 |
| Corely.Billing | 676 | 65 | 176 | 8 |
| DocsToData | 2,901 | 214 | 473 | 56 |

About 5,200 lines become 111 one-line comments.

DocsToData already has the no-comments rule (added in `f183102`), yet it has the most comments. The
rule says existing comments "may not be multiplied", so nothing ever cleared the ones already there.
A rule without a sweep leaves the backlog in place, which is why this plan does both.

## The rule

Adopt DocsToData's rule, which is already written and has the one allowance needed:

> **Write no comments. None, anywhere, including XML doc comments.** Not a summary, not a
> `<param>`, not a note about why. The names and the code carry the meaning; a test carries the
> intent. This is absolute and has no exceptions: if a comment seems necessary, stop, say in chat
> exactly what the comment would be and why the code cannot express it, and wait for explicit
> approval. Do not add it and explain afterwards.

DocsToData's copy ends with "Existing comments may be read, moved or deleted; they may not be
multiplied." Drop that clause once the sweep is done. It exists only for a backlog that will be gone.

### Where it is codified

A path-scoped rule file, `.claude/rules/no-comments.md`, in each of the six repositories:

```markdown
---
paths:
  - "**/*.cs"
  - "**/*.razor"
  - "**/*.cshtml"
---

<the rule above>
```

It loads only when a session works on a matching file, so it costs nothing during docs-only or
script-only work, and it keeps `CLAUDE.md` shorter. Checked in, so it binds every session.

In the same commit, delete each repository's existing comments guidance so nothing contradicts it:

| Repository | Delete from `CLAUDE.md` |
|---|---|
| Corely.Common, Corely.Security, Corely.DataAccess | The `## Comments` section, including its code example |
| Corely.IAM, Corely.Billing | The `### Comments` section under Development Patterns |
| DocsToData | The "Write no comments" bullet, now in the rules file |

Also add `~/.claude/rules/no-comments.md` with the same content, so a repository not in this list
still gets the default. It is private, so it cannot stand in for the repository copies.

`DOCUMENTATION-STYLE.md`'s "Inline Comments in Code" section stays. It governs code samples inside
Markdown docs, which teach a reader and are not source files.

### In scope

`.cs`, `.razor`, `.cshtml`. Comment forms: `//`, `///`, `/* */`, `@* *@`, and `<!-- -->` in Razor
markup. That includes commented-out code, `// Arrange` / `// Act` / `// Assert`, section banners and
the Visual Studio boilerplate header in `GlobalSuppressions.cs`.

### Out of scope

- **`.js`, `.css`, `.ts`.** Few comments, and less-typed languages get more out of them.
- **Scripts, workflows and project files**: `.ps1`, `.sh`, `.yml`, csproj `<!-- -->`.
- **Generated files**: EF migrations, `.Designer.cs`, model snapshots, anything marked
  `<auto-generated>`.
- **Tool directives** that change behavior: `// csharpier-ignore`, `#pragma`, analyzer suppressions.
- **Code samples in `Docs/`.**

## The sweep, per repository

1. Delete every comment in scope, except the ones listed below.
2. Rewrite each kept comment to the minimal text given here. One line, no XML tags.
3. Add `.claude/rules/no-comments.md` and delete the `CLAUDE.md` comments section.
4. `.\RebuildAndTest.ps1` green, then commit.

Order: Corely.Common, Corely.Security, Corely.DataAccess (small, and the others depend on them),
then Corely.Billing, Corely.IAM, DocsToData.

## What stays

The bar: the comment records something the code cannot show, **and** a reasonable cleanup would
break something without it. Usually that means code that looks wrong, redundant or simplifiable
but is not, or a fact about an outside system. Everything else goes, including comments that are
true and well written.

Anchors are the member or statement the comment sits on, not line numbers, since the sweep moves
every line.

### Corely.Common

Nothing.

### Corely.Security

| Where | Minimal comment |
|---|---|
| `ISymmetricEncryptionProvider.ProviderName`, `IAsymmetricEncryptionProvider.ProviderName`, `IHashProvider.ProviderName` | `// Written into every stored value: renaming strands data unless the old name stays a read alias.` |
| `SymmetricEncryptionProviderBase` and `AsymmetricEncryptionProviderBase`, the prefix split in decrypt | `// Prefix not checked against ProviderName: the factory routed by it, and checking would block renames.` |
| `RsaEncryptionProvider.PaddingName` | `// Not ToString(): stored prefixes depend on this exact text.` |
| `FileSymmetricKeyStoreProvider.GetCurrentKey`, `FileAsymmetricKeyStoreProvider.ReadKeys` | `// Bytes, not ReadAllText: a string key can't be zeroed.` |
| `AsymmetricKnownAnswerTests.EcdsaSignature` | `// IEEE P1363 (r‖s), not the DER that ProviderDescription claims.` |
| `StoredFormatCompatibilityTests` class | `// Shipped formats. A failure means stored data is unreadable; never update these literals to pass.` |

The ECDSA row points at a real mismatch: `ProviderDescription` advertises DER, but the output is
P1363. That is a bug report, not a comment. Raise it separately.

### Corely.DataAccess

| Where | Minimal comment |
|---|---|
| `IRepo.ExecuteUpdateAsync` | `// Bypasses the change tracker: set ModifiedUtc yourself.` |
| `EFContextResolver._cache` | `// Per instance, not static: answers depend on this container's registrations.` |
| `ServiceRegistrationExtensions`, the `TryAddScoped<EFUoWProvider>()` registration | `// Concrete registration: EFRepo injects EFUoWProvider; the interface forwards to the same instance.` |
| `AsyncQueryProviderTests`, the explicit `EntityFrameworkQueryableExtensions.ToListAsync` call | `// Called explicitly: .NET 10's AsyncEnumerable.ToListAsync is an equally good match.` |

### Corely.Billing

| Where | Minimal comment |
|---|---|
| `ExpiringFirstGrantSelectionPolicy.Split`, the `ThenBy` chain | `// A total order, so a replay allocates identically.` |
| `IdempotencyKeyFactory` class | `// Composed, not hashed: people read this column during billing disputes.` |
| `IdempotencyKeyFactory`, the max-length check | `// Never truncate: two units of work would share a key.` |
| `ConsumptionProcessor`, the reserve write that skips retry on `DbUpdateException` | `// No retry on DbUpdateException: the failed entity stays tracked, so a retry fails on the tracker.` |
| `ConsumptionProcessor`, `ex is DbUpdateException && await AlreadyRecordedAsync(...)` | `// Lost an insert race: detected by lookup, not provider error codes.` |
| `ReservationOptions.ReservationTtl` | `// Long on purpose: release frees holds; the TTL only catches killed processes and must outlive a next-day replay.` |
| `AsyncLocalOperationContextAccessor._current` | `// Instance, not static: separate accessors must not share a slot.` |

The migration CLI's `CommandBase` help guard is kept the same way as in Corely.IAM, below.

### Corely.IAM

| Where | Minimal comment |
|---|---|
| `BasicAuthProcessorAuthorizationDecorator.CreateBasicAuthAsync`, `.VerifyBasicAuthAsync`; `GoogleAuthProcessorAuthorizationDecorator.GetUserIdByGoogleSubjectAsync`; `TotpAuthProcessorAuthorizationDecorator.VerifyTotpOrRecoveryCodeAsync`, `.IsTotpEnabledAsync` | `// No authorization check, by design: runs before sign-in completes.` |
| `AccountProcessorAuthorizationDecorator.AddUserToAccountForInvitationAsync` | `// No authorization check, by design: the invitation token was already validated.` |
| `AccountProcessorAuthorizationDecorator.ListAccountsAsync` | `// No authorization check, by design: scoped to the caller's own accounts, and needed before any account context exists.` |
| `{Group,Role,Permission,User}ProcessorAuthorizationDecorator`, each list method's resolved scope | `// Never the caller's scope: it could only widen it.` |
| `ListQueryHelper`, `ids.ToList()` | `// Materialized so the provider emits IN (...).` |
| `AuthorizationProvider._cachedPermissions` | `// Absolute, not sliding: active users must still refresh.` |
| `IAuthorizationProvider.GetAuthorizedResourceIdsAsync` | `// null = wildcard (everything); empty = nothing.` |
| `SecurityProvider`, decrypting a stored key; `TotpAuthProcessor`, reading the secret back | `// The provider that wrote it, not the current default: a mismatch looks like a wrong key.` |
| `RegistrationService`, the `EmailVerified` check | `// Sign-in skips this on purpose: it binds by Google subject, not address.` |
| `UserProcessor`, the in-memory account filter on `Groups` / `Roles` | `// Also filtered in memory: mock repos ignore filtered includes.` |
| `UserConstants.EMAIL_MAX_LENGTH` | `// RFC 5321` |
| `SecurityHeadersMiddleware`, where a CSP would be set | `// No CSP: only the host knows its sources.` |
| `PermissionView.razor`, the null-context check | `// Blazor's interim render has no user context yet: unknown, not denied, so don't cache it.` |
| `LinkedAccountsSection.razor`, `google-link-button` | `@* Google's own button: their terms forbid look-alikes. *@` |
| `TotpSection.razor`, the QR `try` | `// Enrolment is already stored; a QR failure must not strand the user.` |
| `CommandBase._showingHelp` in `DataAccessMigrations.Cli` and `DevTools` | `// Help re-invokes this command; guard against recursing when help isn't reachable.` |
| `Demos.UsersOnly` and `Demos.SharedAccount` `Program.cs`, `MapRazorComponents` | `// Corely.IAM.Web's assembly omitted on purpose: it would route the admin pages.` |
| Both demos' `Pages/Shared/_AuthLayout.cshtml` | `@* Keep form-busy.js: submit buttons depend on it. *@` |
| Both demos' `Home.razor`, the scoped `ExecuteDeleteAsync` | `// The id is client input.` |
| `AuthorizationProviderTests.RevokeAllPermissionsAsync` | `// Deletes rather than clearing flags: MockRepo shares instances with the cache.` |
| `UserListLoadingStateTests`, `contextGate` | `// Held open: a synchronous mock never produces the interim render the bug lived in.` |

### DocsToData

**Admin portal and its tests**

| Where | Minimal comment |
|---|---|
| `AdminPortalWebApp.Tests/AssemblyInfo.cs` | `// Both tiers reuse one SQL container locally; in parallel they tear it down under each other.` |
| `PortalFactory` and `PortalBrowserFixture`, `AzureStorage__AccountName` | `// Explicitly blank: omitted, a dev appsettings.json points this at real Azure.` |
| `PortalFactory` class | `// Env vars, not ConfigureAppConfiguration: Program reads config before Build().` |
| `PortalDatabase`, loading the IAM and Billing scripts | `// From corely-iam-db / corely-billing-db db script -i -p MsSql; regenerate on package bumps.` |
| `PortalDatabase` and `Functions.Tests/Host/HostDatabase`, the interpolated `CREATE DATABASE` | `// Name comes from our own config; CREATE DATABASE can't be parameterized.` |
| `AdminPortalWebApp/Program.cs`, `if (builder.Environment.IsLocal())` | `// Environment is "local", not "Development", so CreateBuilder skipped these.` |
| `AdminPortalWebApp/Program.cs`, the startup `throw;` | `// Rethrow: swallowing makes a failed startup exit 0.` |
| `DocumentWorkflowEditor.razor.cs` and `ExtractionTemplateEditor.razor.cs`, the `!_loading` guard | `// Not while loading: attaching to a list not yet rendered kills the circuit.` |
| `ExtractionTemplateEditor.razor.cs`, reordering `_fields` after a drag | `// Match Sortable's DOM before the next render, or Blazor's diff fights it.` |
| `JsonEditor.razor`, the `_lastReportedValue` check | `// Don't echo the editor's own value back: it moves the caret.` |
| `GrantEditor.razor.cs`, awaiting the user context | `// Before rendering: PermissionView won't recheck once the context arrives.` |
| `Usage.razor.cs`, the sequential loads | `// Sequential: one DbContext.` |

**Core, extraction and providers**

| Where | Minimal comment |
|---|---|
| `QuotaAuthorizationDecorator`, returning `Unknown` | `// Unknown, not Exhausted: a denial says nothing about quota.` |
| `DocumentExtractionService`, `GetUserContext()!` | `// User is null under the Functions host's system context.` |
| `DocumentExtractionService`, one result code for no grants and not enough | `// Same code for both: don't reveal how we knew.` |
| `DocumentExtractionService`, `Math.Max(providerPages, preflightPages ?? 0)` | `// Bill the higher count: a divergence means one side is wrong.` |
| `DocumentBuffer.OpenReadAsync` | `// Same instance each call; callers must not dispose it.` |
| `IDocumentPageCounter.TryGetPageCountAsync` | `// Null when unreadable (encrypted, malformed); never throws for that.` |
| `DocsToDataEnvironments` class | `// Never use IsDevelopment/IsProduction: environments are local/dev/stg/prod.` |
| `ProcessRunner`, the three empty `catch` blocks | `// Throws once the process has exited.` |
| `AmazonTextractClientExecutor`, `AuthenticationRegion` | `// ServiceURL disables region inference; signing still needs it.` |
| `TextractOptions.MaxPages` | `// Not a typo: AWS's fixed quota for synchronous AnalyzeDocument is 1 page.` |
| `MistralDocumentExtractionProvider`, `documentUrl = null!` | `// Release the ~15 MB string: async locals live on the state machine.` |
| `MistralDocumentExtractionProvider`, the local-only body logging | `// Local only: body logging buffers the whole payload.` |
| `MistralDocumentExtractionProvider`, reading `UsageInfo` | `// 2512 counts pages_processed_annotation; OCR 4 counts pages_processed.` |
| `MistralOcrRequestContent`, `UnsafeRelaxedJsonEscaping` | `// Safe for base64, which needs no escaping; avoids ~6x buffering.` |
| `MistralOcrResponse` class | `// Only pages and document_annotation are relied on; models disagree on the rest, so it stays optional.` |
| `OcrRequestDtos.cs`, top of file | `// https://docs.mistral.ai/api/#tag/ocr` |
| `MistralDocumentAIOptions`, where `WordConfidence` is resolved | `// Not inherited: a model without word scores may reject the request.` |
| `PdfSharpDocumentPageCounter`, `PdfDocumentOpenMode.Import` | `// Import: InformationOnly is obsolete in PDFsharp 6.` |

**Workflow orchestration and Functions**

| Where | Minimal comment |
|---|---|
| `DocumentWorkflowExecutionService.IdempotencyScopeFor` | `// Identical across retries and redeliveries, or retries double-charge.` |
| `DocumentWorkflowExecutionService`, the stale-message branch | `// Don't republish: a stale message can't tell a lost publish from a redelivery. The recovery sweep handles strands.` |
| `DocumentWorkflowExecutionService`, `BeginScope` around the retry | `// Around the retry, so every attempt shares one billing identity.` |
| `DocumentWorkflowExecutionService`, the rethrow after recording a failure | `// Rethrow so the message redelivers instead of the job vanishing.` |
| `DocumentWorkflowFinalizationService`, creating the parent directory | `// HNS rejects a rename into a missing parent, and nothing else creates it.` |
| `ExtractionStepRunner.RenameDocumentAsync` | `// New blob, then row, then delete old: a crash leaves a stray blob, never a dangling row.` |
| `ExtractionStepRunner.ResolveStemAsync`, the fallback | `// Already paid for: fall back to the source name rather than fail.` |
| `SplitStepRunner` class | `// Children go outside the parent's directory: finalizing renames it.` |
| `SplitStepRunner`, `overwrite: true` | `// Overwrite: a replay writes the same path.` |
| `ChildJobIdFactory` class | `// Deterministic, not v7: a replay must reproduce child paths and idempotency keys.` |
| `ChildJobIdFactory`, `bigEndian: true` | `// bigEndian so the RFC byte positions above apply.` |
| `Functions/Program.cs`, the logger filter removal | `// App Insights adds a Warning+ filter by default.` |

**Test infrastructure**

| Where | Minimal comment |
|---|---|
| `FunctionsHostFixture`, `APPLICATIONINSIGHTS_CONNECTION_STRING` | `// Must parse, or the worker dies with an error that reads like a database failure.` |
| `FunctionsHostFixture`, the startup rethrow | `// Started then failed: the app is broken, so fail rather than skip.` |
| `RecoverySweepTests`, `Task.Delay` | `// Waiting for something not to happen: long enough for a republish to have run.` |
| `WorkflowPipelineTests`, resetting `Mistral.StatusCode` | `// Shared stub: reset it or later tests fail.` |
| `DocumentJobRelationshipTests`, the orphaned-row test | `// Pins the absence of foreign keys; delete this test if they're added.` |
| `LocalStubs/Program.cs` | `// Ports match every appsettings.local.json and local.settings.json; change them together.` |
| `TestPdf` class | `// Blank pages: PDFsharp has no font resolver on the Linux CI runner.` |
| `MistralOcrStub.BuildOcrResponse` | `// document_annotation is a JSON string, not an object.` |
| `TextractAnalyzeStub`, the error body | `// The SDK maps __type to a typed exception.` |

## Done when

All six repositories have the rules file, no comments section in `CLAUDE.md`, only the comments
listed above, and a green `RebuildAndTest.ps1`. `~/.claude/rules/no-comments.md` exists. This plan
moves to `Plans/Completed/` with an Outcome section.

## Outcome

Done in one pass across all six repositories. Comments were removed by a lexer-based script rather
than by hand, so strings containing `//` survived and trailing comments went too, then the kept
comments were written back at their anchors.

| Repository | Files touched | Comment lines removed | Comments kept |
|---|---:|---:|---:|
| Corely.Common | 16 | 165 | 0 |
| Corely.Security | 38 | 274 | 10 |
| Corely.DataAccess | 48 | 231 | 4 |
| Corely.IAM | 129 | 886 | 33 |
| Corely.Billing | 65 | 668 | 8 |
| DocsToData | 214 | 2,882 | 54 |

Two differences from the list above:

- `ProcessRunner` got one comment above its three `try` blocks instead of one per empty `catch`.
- One CSS comment inside a `<style>` block in `DocumentWorkflowEditor.razor` stays. CSS is out of
  scope; it just happens to live in a Razor file.

The ECDSA mismatch in Corely.Security (`ProviderDescription` says DER, the output is P1363) is still
open and was not touched here.
