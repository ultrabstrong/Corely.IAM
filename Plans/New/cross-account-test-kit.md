# Cross account test kit

## Goal

Give any app using Corely.IAM a small, optional way to prove its own rows are kept to their account.
IAM leaves that scoping to the app on purpose, as `Docs/gotchas.md` explains, so this adds nothing to
an app's production code.

## What it is

A test only package, `Corely.IAM.Testing`, referenced by an app's test project and never by the app.
It holds the check that IAM, Corely.Billing and DocsToData each wrote for themselves:

1. Given service interfaces, list every public method.
2. Fail when a method has neither an attack nor a written exemption with its reason.
3. Run each attack as a caller in another account with every permission granted, and report any that
   were not refused.

The app supplies its services, its attacks, how to set up the caller, and what counts as refused,
because result shapes belong to the app.

## Out of scope

Anything an app's production code would have to reference. No base context, no marker interfaces, no
required shape for services.

## Tests

Unit tests in this repository: a method with no case fails, an exempt method without a reason fails,
and an attack that gets through is reported by name.

## Order

1. Extract the shared check from `CrossAccountCoverageTests` here.
2. Move this repository's coverage test onto it.
3. Link it from `Docs/gotchas.md`.
4. Move DocsToData's and Billing's coverage tests onto it, each proving the test still goes red with its
   fix removed.
