# Microsoft

OpenID Connect against the Microsoft identity platform, for work and personal accounts. A
certificate is the preferred credential: Microsoft advises against client secrets in production.

## Register

```powershell
az ad app create --display-name "My App" --sign-in-audience AzureADandPersonalMicrosoftAccount `
    --web-redirect-uris "https://<host>/signin/callback/microsoft"
```

Then, on the app registration:

1. Add the optional ID token claims `email` and `xms_edov`.
2. Upload a certificate's public key (Certificates and secrets), or create a client secret. A secret
   lasts at most 24 months; Microsoft recommends under 12.

## Configure

```json
"Microsoft": {
  "ClientId": "<application id>",
  "CertificatePfxBase64": "<PFX with its private key, base64>",
  "CertificatePassword": "<PFX password>",
  "Tenant": "common"
}
```

`ClientSecret` replaces the two certificate keys when a secret is used. `Tenant` is `common` (work and
personal accounts), `organizations`, `consumers`, or one tenant ID.

## Notes

- A user is identified by issuer and subject, and the issuer carries the tenant, so the same subject
  in two tenants is two people. The token's `iss` must equal
  `https://login.microsoftonline.com/{tid}/v2.0` with its own `tid`.
- The email counts as verified for a personal account, or when `xms_edov` is true. A work account
  without either can be linked but cannot sign up.
- IAM signs the client assertion with the certificate (`private_key_jwt`).
- Proving it is you sends `prompt=login` and `max_age=0`, and the returned `auth_time` must be recent.
