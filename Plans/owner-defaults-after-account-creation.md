# Owner defaults after account creation

**Status: parked.** Recorded so it is not lost; not planned for any release.

Comes out of `explicit-permission-resource-types.md`, where a host's owner defaults are given to
`RegisterResourceType` and applied, as system-defined permissions, only when an account is created.
That leaves two gaps for accounts that already exist.

## A host adds a feature

A host registers a new resource type, or starts giving owners a capability they did not have, after
accounts already exist. Those accounts' owners get nothing from the new owner default. The host can add
the permission to each Owner role under system context, but it lands as an ordinary row: it is not
protected like the owner defaults, so it depends on phase 2's rule that an owner cannot detach what their
role needs.

## A host changes an owner default

Changing a type's owner default, for example from `cRUdX` to `CRUDX`, affects accounts created afterwards.
Existing accounts keep the rows they were created with, and system-defined rows cannot be changed or
removed by anyone.

## Starting points, not decisions

- A way to apply the current owner defaults to existing accounts, as system-defined rows, run by the
  host on purpose. It has to be a service method: the migration CLI never sees a host's registered
  types. Phase 1 of `explicit-permission-resource-types.md` does not build one; its only existing
  database is fixed by hand.
- Whether a changed default narrows existing rows, or only ever adds.
