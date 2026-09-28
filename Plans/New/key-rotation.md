# Rotate user and account keys

**Status: parked.** Recorded so the scoping is not lost. Nothing is built.

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
