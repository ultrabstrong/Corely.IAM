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

**Update: phase 2 shipped in Corely.IAM 3.0.0, and the case is refused.**
`GrantOnlyWhatYouHoldTests.Delegate_CannotAssignTheOwnerRole_ToThemselves` holds a member with
`permission: C`, `role: U`, `user: U` and `group: U` and shows assigning the Owner role to themselves
is refused. What remains open is only whether a member who does hold everything the Owner role holds
should still be stopped from taking it; nothing here decides that.
