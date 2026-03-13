# EncryptionSigningPanel

Tabbed interface for testing encryption and signing operations using account or user key providers. Used on the Account Detail and Profile pages.

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `SymProvider` | `IIamSymmetricEncryptionProvider?` | — | Symmetric encryption provider |
| `AsymProvider` | `IIamAsymmetricEncryptionProvider?` | — | Asymmetric encryption provider |
| `SigProvider` | `IIamAsymmetricSignatureProvider?` | — | Digital signature provider |

## Usage

```razor
<EncryptionSigningPanel
    SymProvider="@_symProvider"
    AsymProvider="@_asymProvider"
    SigProvider="@_sigProvider" />
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
- Provider name and description shown as badges
