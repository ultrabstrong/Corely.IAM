# Google

OpenID Connect, with a client secret. Google says whether it verified the email (`email_verified`).

## Register

1. In the Google Cloud Console, open APIs and Services, then Credentials, and create an OAuth client ID
   of type Web application. Google has no CLI for OAuth web clients.
2. Add the redirect URI `https://<host>/signin/callback/google`. For local work,
   `https://localhost:<port>/signin/callback/google` is allowed.
3. Copy the client ID and create a client secret.

## Configure

```json
"Google": { "ClientId": "<id>.apps.googleusercontent.com", "ClientSecret": "<secret>" }
```

## Notes

- IAM requests `openid email profile` and sends PKCE (S256), `state` and `nonce`.
- Google sends RFC 9207's `iss` with its response, and IAM rejects one naming another issuer.
- Google does not offer a forced re-entry of credentials, so proving it is you accepts Google's
  current session.
