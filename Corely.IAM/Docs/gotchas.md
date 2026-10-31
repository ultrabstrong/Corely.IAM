# Gotchas

Corely.IAM sits beside your app. It does not own your tables, your queries or your code's shape, so
it leaves some decisions to you on purpose. Each entry here says what goes wrong if you miss one, what
to do instead, and why the library does not do it for you.

## Permissions do not keep accounts apart in your own tables

**What goes wrong.** A permission on `Guid.Empty` covers every resource of its type, and
`IsAuthorizedAsync` has no way to know which account one of your rows belongs to. So this passes for
the owner of any account, whichever account the row is in:

```csharp
await authorizationProvider.IsAuthorizedAsync(AuthAction.Update, "invoice", invoiceId);
var invoice = await db.Invoices.SingleAsync(i => i.Id == invoiceId);
```

An owner who gets hold of another account's id can read or change that row.

**What to do.** Treat the permission check and the account check as two separate steps. Scope every
lookup by id to the caller's account, from the user context:

```csharp
var accountId = userContextProvider.GetUserContext()!.CurrentAccount!.Id;
var invoice = await db.Invoices.SingleOrDefaultAsync(
    i => i.Id == invoiceId && i.AccountId == accountId
);
```

Answer a row from another account the same way as one that does not exist, so an id cannot be used to
find out what exists. Rows with no `AccountId` of their own, such as a line on an invoice, are scoped
through the parent that has one. It costs no extra query: the filter goes in the query you already run.

**Prove it.** Write a test that names another account's ids on every method that takes an id, with
every permission granted, and checks that nothing comes back or changes. Make it fail when a new
method has no such case, so the next method cannot be forgotten. The test only package
`Corely.IAM.Testing` holds that second half:

```csharp
var coverage = CrossAccountCoverage.Check(
    [typeof(IInvoiceService)],
    attacks.Keys,
    new Dictionary<string, string> { ["IInvoiceService.CreateAsync"] = "Takes no id" }
);
Assert.True(coverage.IsComplete, coverage.ToString());

var reached = await CrossAccountCoverage.FindReachedAsync(attacks);
Assert.Empty(reached);
```

Each attack calls one method as a caller in another account, with every permission granted, and
returns whether it was refused. Your test supplies the attacks because your results are your own.
Reference the package from your test project, never from your app.

**Why IAM does not do it for you.** Your rows are yours. IAM knows a resource type and an id, not your
schema, and covering this for you would mean owning your data layer: a required base class, marker
interfaces on your entities. That would bake IAM into your app instead of sitting alongside it.

## System context passes every check

**What goes wrong.** `AuthenticateAsSystem` passes `IsAuthorizedAsync` and `HasAccountContext` for
every account, and it has no `CurrentAccount`. A background job acting on ids that came from outside,
such as a file path, a queue message or a stored reference, has nothing checking that those ids belong
together. One account's upload could run against another account's configuration, or spend its quota.

**What to do.** When system code handles ids from outside, prove the ownership yourself: compare the
account on each row before you act on it.

**Why IAM does not do it for you.** System context exists to work across accounts, and only your code
knows which of your ids should agree.

## API keys start with access to nothing

**What goes wrong.** `ApiKeyResourceTypes` starts empty. A key is refused on every resource type until
the platform allows the type, whatever the key's roles hold, so a correctly built role can still answer
403.

**What to do.** Switch on each type the key needs, in the platform's security settings or in
`SecurityOptions`. Register your own types first, so they can be chosen.

**Why IAM does not do it for you.** Which of your resources a program may reach is your decision, and
the safe default is none.

## Changing owner actions does not reach existing accounts

**What goes wrong.** Owner defaults only ever add. Remove an action from a type's owner actions and
every existing account keeps it, because its row is system defined and stays on the Owner role. Only
accounts created afterwards get the new default.

**What to do.** When you take an action away, edit the existing rows yourself as part of the change.

**Why IAM does not do it for you.** Taking access away from people automatically is not something a
library should do at startup. See [Owner Defaults](resource-types.md#owner-defaults).

## Rate limits are yours

**What goes wrong.** IAM ships no limits, so token endpoints and your API accept as many calls as
they receive. The built in limiters also count per instance, so several instances each allow the
full limit.

**What to do.** Add a limiter, as [Rate limiting](../../Corely.IAM.Web/Docs/bearer-api.md#rate-limiting)
shows, and a distributed one if instances must share a limit.

**Why IAM does not do it for you.** The right limit depends on your traffic and your tiers.
