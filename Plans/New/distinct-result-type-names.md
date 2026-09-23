# Distinct result type names across Corely libraries

## The problem

Corely.IAM and Corely.Billing each define the same six shared result types, under the same names,
in namespaces that differ only by library:

| Type | `Corely.IAM.Models` | `Corely.Billing.Models` |
|---|---|---|
| `RetrieveResultCode` | Success, NotFoundError, UnauthorizedError | Same members |
| `ModifyResultCode` | + SystemDefinedError, ValidationError, UsernameExistsError, EmailExistsError | Success, NotFoundError, UnauthorizedError, ValidationError |
| `ModifyResult` | `(ResultCode, Message)` | Same shape |
| `RetrieveSingleResult<T>` | `(ResultCode, Message, Item, EffectivePermissions)` | `(ResultCode, Message, Item)` |
| `RetrieveListResult<T>` | `(ResultCode, Message, Data)` | Same shape |
| `PagedResult<T>` | Identical | Identical |

A file that imports both namespaces gets an ambiguity error and needs an alias (DocsToData's
ConsoleApp has `using BillingRetrieveResultCode = Corely.Billing.Models.RetrieveResultCode;`). Even
where no alias is needed, two unrelated types with one name across a codebase is a reading hazard.

## Decided: keep them separate

Moving them into Corely.Common was considered and rejected. The table already shows the two sets
diverging (`ModifyResultCode`, `RetrieveSingleResult<T>`), and each library must be free to add a code
or a field without a release of the other. Sharing would couple two libraries through their smallest,
most frequently changed types.

The fix is names, not location: each library's types say whose they are.

## Options

- **(a) Billing renames, IAM keeps its names.** `BillingRetrieveResultCode`, `BillingModifyResult`,
  and so on. IAM is older and has more consumers; only one side has to move to end the collision.
  Billing is still a preview, so the break is cheap there.
- **(b) Both rename, with a library prefix.** `IamRetrieveResultCode` alongside
  `BillingRetrieveResultCode`. Symmetric and future-proof for a third library, but a breaking change
  across every IAM consumer (Corely.IAM.Web, the demos, DocsToData) for no collision IAM causes on
  its own.
- **(c) Domain names instead of library prefixes**, e.g. `GrantRetrieveResult`. Rejected: the types
  are generic over the domain, and a per-domain name multiplies them.

**Recommendation: (a),** and adopt the rule for any future Corely library: shared result types carry
the library's name, except in Corely.IAM, which keeps the unprefixed names it already published.

`PagedResult<T>` is the one type that is truly identical and has no domain meaning. Renaming it in
Billing is consistent with (a); leaving it duplicated is also acceptable. Decide when doing the work.

## Work, for (a)

1. Corely.Billing: rename the six types in `Corely.Billing/Models/` and every use (library, tests,
   docs, `Docs/result-codes.md`, the ConsoleTest demo). A mechanical rename; the compiler finds every
   site.
2. Bump Billing's minor preview version and release (ask before tagging).
3. DocsToData: bump the package, update the uses in `DocsToData.Authorization`, the portal pages and
   ConsoleApp, and delete the `BillingRetrieveResultCode` alias.
4. Record the naming rule in Corely.Billing's `DESIGN-DECISIONS.md`, and in this repository's if (b)
   is ever chosen.

For (b), add: rename in Corely.IAM and Corely.IAM.Web, a major version bump of IAM, and the same
update pass in every IAM consumer.

## Done when

No two Corely libraries publish a type with the same simple name, DocsToData compiles with no
namespace alias for either library, and the rule for future libraries is written down.
