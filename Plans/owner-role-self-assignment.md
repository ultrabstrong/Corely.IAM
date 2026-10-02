# Owner role self-assignment

**Status: parked.** Recorded so it is not lost; not planned for any release.

Anyone holding Update on `user` and Read on `role` in an account can assign any role to any member,
themselves included, and that includes the Owner role. Nothing in `UserProcessor.AssignRolesToUserAsync`
or `GroupProcessor.AssignRolesToGroupAsync` treats the Owner role specially, and the authorization
decorators check only Update on the user or group and Read on the roles.

So a member given those two permissions for ordinary user management can make themselves an owner. In
DocsToData only owners hold them today, so nobody can use it yet.

Phase 2 of `explicit-permission-resource-types.md` (grant only what you hold) would refuse the
assignment, because the caller would not hold every permission of the Owner role. Whether that is the
whole answer, or the Owner role needs a rule of its own, is for this plan to decide.
