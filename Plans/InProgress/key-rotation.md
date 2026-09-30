# Rotate user and account keys

**Status: built, not yet clicked through.** Built in a cloud session with every test tier green,
including the provider matrix against the new migration. Before this moves to `Completed/`, click
through Rotate key in the WebApp on Profile and on Account Detail: rotate each of the three keys,
confirm text encrypted before still decrypts and a signature made before still verifies, and that
you stay signed in after rotating your own signing key.

## The ask

Users and accounts each get a symmetric encryption key, an asymmetric encryption key pair and a
signing key pair, provisioned at registration and never replaced. Having keys that cannot be rotated
is a strange choice. Add a way to regenerate them:

- a user's three keys
- an account's three keys
- one active key per purpose; multiple active keys are not needed

## What exists

`Corely.Security` already has versioned key stores and `ReEncrypt`, so the primitives are there.
What is missing is `Corely.IAM` exposing rotation: there is no `Rotate` or `Regenerate` on
`IModificationService` or the processors.

## Decide before building

These settle whether this is a small change or a large one.

1. **What happens to data encrypted with the old key?** "One active key" must not mean "discard the
   old one": that destroys access to everything already encrypted with it. Either keep old versions
   for decryption only (the key store already versions), or re-encrypt what IAM stores and accept
   that callers holding ciphertext elsewhere must re-encrypt theirs.
2. **Signatures made with the old signing key.** Verification against the new public key fails.
   Keep old public keys available for verification, or document that rotation invalidates past
   signatures.
3. **Which permission gates it?** Rotating an account's keys is a different authority from rotating
   your own. Likely `account` Update for the account, and a self operation (`IsNonSystemUserContext`)
   for the user.
4. **UI in the first pass or service only?** If UI, a Rotate button per key on the Encryption and
   Signing panel, with a confirm explaining point 1.

## Decided

1. **Keep old versions.** Rotation adds a generation; nothing is deleted. Old ciphertext decrypts,
   and `ReEncrypt` moves a value onto the newest generation when its holder chooses.
2. **Keep old public keys.** Earlier signatures still verify, and sign in tokens are validated against
   every signing generation, so rotating your own signing key does not end your sessions.
3. **Permissions as proposed.** Account keys need `account` Update; user keys are a self operation.
4. **Service and UI in the first pass.**

## What was built

- A `Generation` column on the four key tables, with the unique index moved to (owner, purpose,
  generation). Existing keys become generation 1. The migration creates the new index before dropping
  the old one, because MySQL will not drop an index a foreign key relies on. Rolling it back fails
  once any key has been rotated, rather than silently dropping a generation.
- The existing `Version` column is the system key's version, not the key's own, which is why a new
  column was needed.
- Providers hold every generation in a Corely.Security in-memory key store, so the version in each
  ciphertext maps to its generation. Generations must run from 1 without gaps; anything else is a
  fault.
- Each generation keeps its own provider name, so a rotation that also changes the default algorithm
  still decrypts and verifies what came before.
- `IModificationService.RotateAccountKeyAsync` and `RotateCurrentUserKeyAsync`, through a new
  `KeyRotationProcessor` with authorization and telemetry decorators.
- A Rotate key button on each tab of the Encryption and Signing panel, with a confirm, on Profile and
  (with Update permission) Account Detail.

## Left out

- `GetAsymmetricSignatureVerificationKeyAsync` returns only the newest public key. A host verifying
  signatures outside IAM that needs older keys would need a way to list them.
- User Detail shows the panel for your own user without the Rotate button; Profile has it.
