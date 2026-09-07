# Decrypt with the provider the value names

## The incident

A deployed app running Corely.IAM 2.0.1 could not sign in. Password verification passed, then
`CreateUserAuthTokenAsync` threw:

```
System.Security.Cryptography.AuthenticationTagMismatchException
  System.Security.Cryptography.AesGcm.DecryptCore
  Corely.Security.Encryption.Providers.AesGcmEncryptionProvider.DecryptInternal
  Corely.Security.Encryption.Providers.SymmetricEncryptionProviderBase.Decrypt
  Corely.IAM.Security.Providers.SecurityProvider.DecryptWithSystemKey
  Corely.IAM.Security.Providers.AuthenticationProvider.CreateUserAuthTokenAsync
```

Every encrypted value in that database is prefixed `AES-256-CBC-PKCS7:1:`, written by Corely.IAM
1.x. The consumer upgraded to 2.0.0 (Corely.Security 3.0.0), where the default symmetric provider is
AES-GCM. Nothing about the data changed; the provider used to read it did.

The failure reads as a wrong key — an authentication tag mismatch is what a wrong key looks like —
and it cost most of a day chasing a key that was never wrong. A developer deleted a TOTP enrollment
and rotated a Key Vault secret before anyone looked at what the ciphertext said about itself.

## The defect

`SymmetricEncryptedValue` stores `providerName:keyVersion:base64Cipher`, and
`ISymmetricEncryptionProviderFactory.GetProviderForDecrypting(value)` exists precisely so a stored
value is read back by the provider that wrote it. `Docs/provider-factories.md` lists it under "Auto
resolution (verification / decryption)".

Two decrypt paths do not use it:

| Location | Currently | Should be |
|---|---|---|
| `Corely.IAM/Security/Providers/SecurityProvider.cs:150` (`DecryptWithSystemKey`) | `GetDefaultProvider()` | `GetProviderForDecrypting(encryptedValue)` |
| `Corely.IAM/TotpAuths/Processors/TotpAuthProcessor.cs:301` (`DecryptWithSystemKey`) | `GetDefaultProvider()` | `GetProviderForDecrypting(encryptedValue)` |

The pattern is already correct elsewhere, which is what makes these two look like oversights rather
than a design choice:

- `Corely.IAM/Security/Mappers/EncryptedValueMapper.cs:18` → `GetProviderForDecrypting(source)`
- `Corely.IAM/Security/Mappers/HashedValueMapper.cs:18` → `GetProviderToVerify(source)`

Every other `GetDefaultProvider()` call in `SecurityProvider` is on an **encrypt** path, where the
default is the right answer. Only decrypt needs to honour what is stored.

## The change

Two call sites. Encryption keeps using the default, so new values are written with the current
provider and old values stay readable — the same upgrade-on-write shape `NeedsRehash` already gives
hashing.

## Tests

The regression to prove is "a value written by an older default is still readable after the default
changes". State it in those terms rather than naming CBC, so the test keeps its meaning the next
time the default moves:

- Encrypt with a non-default provider, then decrypt through `DecryptWithSystemKey` with a different
  default configured. Fails today with `AuthenticationTagMismatchException`.
- Same for `TotpAuthProcessor`, whose `EncryptWithSystemKey`/`DecryptWithSystemKey` pair is private
  and reachable through `VerifyTotpOrRecoveryCodeAsync`.
- Confirm each test fails before the fix. A test written against fixed code is a guess about what
  would have failed.

## Worth considering separately

- **Nothing warned on upgrade.** The 2.0 migration notes describe an API change; they do not say the
  default symmetric provider changed, which is a data-compatibility change and the one that hurt.
  `MIGRATION-2.0.md` in Corely.Security is the place to say so.
- **A grep for `GetDefaultProvider()` on any decrypt or verify path** would find the rest of this
  class of bug in one pass. Two were found by walking a single stack trace, so the search was never
  systematic.
- **The exception could name the problem.** `SymmetricEncryptionProviderBase.Decrypt` knows the
  provider it is and can read the provider the value names. When they differ, saying so — rather
  than letting AES-GCM throw a tag mismatch — turns a day of misdiagnosis into a sentence.

## Consumer side

DocsToData is migrating its existing values from CBC to GCM with a one-off script, so it does not
depend on this fix to unblock. The fix still matters: any other database written by 1.x is in the
same state, and the trap is armed again for the next default change.

## Status

Fixed in `Corely.IAM` 2.1.1 and `Corely.Security` 3.0.2.

- Both call sites now use `GetProviderForDecrypting`. Encryption still uses the default, so new
  values are written with the current provider and old values stay readable.
- Both regression tests were confirmed failing first. `SecurityProviderDecryptTests` failed at
  `SecurityProvider.cs:151` - the same line as the incident stack trace.

The three follow-ups were all worth doing, and one of them corrected the plan:

- **The systematic grep was run.** All ten `GetDefaultProvider()` calls in `Corely.IAM` were
  checked. The two named here were the only defects; every other call is on an encrypt or
  hash-creation path where the default is correct. `HashedValueMapper` already splits correctly -
  `GetProviderToVerify` when reading, `GetDefaultProvider` when writing.
- **The exception now names the problem.** `SymmetricEncryptionProviderBase.Decrypt` catches a
  `CryptographicException` and, when the stored value names a different provider, says which one
  wrote it and which one is reading. Deliberately only on the failure path: the existing comment
  explaining why the prefix is *not* checked up front is a considered decision about provider
  renaming, and blocking there would undo it.
- **The migration note went to the wrong repository in this plan.** It says `MIGRATION-2.0.md` in
  Corely.Security, but the default was never changed there - `Corely.IAM` picks it, in commit
  `642253b2`, first shipped in **1.3.0**. The note is in `Corely.IAM/MIGRATION-2.0.md`, which is
  also where someone upgrading 1.1.1 to 2.0.0 would look.
