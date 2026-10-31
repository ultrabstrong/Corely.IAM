# EncryptionSigningPanel

Tabbed interface for testing encryption and signing operations using account or user key providers. Used on the Account Detail and Profile pages.

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `SymProvider` | `IIamSymmetricEncryptionProvider?` | none | Symmetric encryption provider |
| `AsymProvider` | `IIamAsymmetricEncryptionProvider?` | none | Asymmetric encryption provider |
| `SigProvider` | `IIamAsymmetricSignatureProvider?` | none | Digital signature provider |
| `RotateKeyAsync` | `Func<KeyType, Task<ModifyResult>>?` | none | Rotates the key on the current tab. The Rotate button shows only when set |
| `RevealKeyAsync` | `Func<KeyType, string?, string?, Task<RevealAccountKeyResult>>?` | none | Returns the key on the current tab, given the password and two factor code. The copy action shows only when set |

## Usage

```razor
<EncryptionSigningPanel
    SymProvider="@_symProvider"
    AsymProvider="@_asymProvider"
    SigProvider="@_sigProvider" />
```

Profile passes `RotateKeyAsync` for the signed in user's keys. Account Detail passes it only to users with Update permission on the account. Both reload their providers after a successful rotation:

```razor
<EncryptionSigningPanel
    SymProvider="@_symProvider"
    AsymProvider="@_asymProvider"
    SigProvider="@_sigProvider"
    RotateKeyAsync="RotateKeyAsync" />
```

## Tabs

### Tab 1: Symmetric Encryption
- Input textarea for plaintext or ciphertext
- **Encrypt**, **Decrypt**, **Re-encrypt** buttons
- Output textarea (read-only) with copy button

### Tab 2: Asymmetric Encryption
- Read-only public key display with copy button
- Input textarea for plaintext or ciphertext
- **Encrypt**, **Decrypt**, **Re-encrypt** buttons
- Output textarea with copy button

### Tab 3: Digital Signature
- Read-only public key display with copy button
- Payload textarea
- Signature textarea
- **Sign**, **Verify** buttons
- Verification result: green for valid, red for invalid

## Behavior

- Crypto operations run on the thread pool via `Task.Run()` to avoid blocking the UI
- Copy buttons show a green checkmark for 1.5 seconds, then revert
- Each tab operates independently with its own state
- Error messages displayed as inline alerts
- Provider name, the current key version and the provider description shown at the top of each tab
- **Rotate key** asks for confirmation first, explaining that earlier ciphertext still decrypts and earlier signatures still verify, then reports success or the failure message
- **Copy key** and **Copy private key** sit immediately left of Rotate key on each tab, on the account panel only. They ask for the password and two factor code beside a warning that this is usually unnecessary, that anyone holding the key can read or sign as the account, and that the key is a secret to store securely and not share with anyone untrusted. The warning stays beside the key
- A shown key clears after 60 seconds, on **Hide**, on changing tab, when rotating and when the page closes. The clipboard keeps what was copied
