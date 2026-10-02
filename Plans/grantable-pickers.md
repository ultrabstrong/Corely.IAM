# Grantable pickers

**Status: not started.**

## The problem

IAM refuses any grant the caller does not fully hold ("Cannot grant permissions you do not hold").
The portal does not know that ahead of time. Every picker lists everything in the account, and
choosing something the caller cannot grant fails only after they submit. A member with delegated
access sees a list mostly made of choices that will be refused.

| Page | Picker or form | Hands out |
|------|----------------|-----------|
| `RoleDetail` | Add permissions | Those permissions |
| `UserDetail` | Add roles | Every permission of those roles |
| `UserDetail` | Add groups | Every permission of every role of each group |
| `GroupDetail` | Add roles | Every permission of those roles |
| `GroupDetail` | Add users | Every permission of every role of this group, for each user added |
| `PermissionList` | Create permission | The actions ticked, on the type chosen |

## What is wanted

A caller sees only what they could actually hand out. Nothing is offered that would be refused.

## Starting points, not decisions

- **The rule already exists.** `IAuthorizationProvider` has `CanGrantAsync`,
  `CanGrantPermissionsAsync`, `CanGrantRolesAsync` and `CanGrantGroupAsync`, but each answers yes or
  no for a whole set. A picker needs the answer per item: something like "which of these ids can I
  grant", computed from one load of the caller's permissions rather than one call per row.
- **Paging.** The pickers load a page at a time through `ListPermissionsAsync`, `ListRolesAsync` and
  so on. Filtering a page after it loads leaves short or empty pages. Either the list queries take a
  "grantable by the caller" filter, or the picker loads candidates whole and pages in memory.
  Accounts hold tens of these, not thousands, so the second may be enough.
- **Add users to a group** is the odd one out: whether it can be granted depends on the group, not on
  the user picked. Either every user is offered or none are, so the Add users button itself is what
  hides.
- **Create permission:** offer only the resource types and actions the caller holds, on
  `Guid.Empty` or the id chosen.
- **The server check stays.** Filtering is a convenience; the decorators remain the guarantee.
- **System context** sees everything, as it does today.

## Done when

On each surface above, a caller with partial delegation sees only what they can grant, an owner
sees everything within the owner defaults, and the refusal message is reachable only by a request
the portal did not offer.
