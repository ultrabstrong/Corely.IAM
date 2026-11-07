# GitHub

OAuth 2.0 through a GitHub App, with a client secret.

## Register

1. In GitHub, open Settings, then Developer settings, then GitHub Apps, and create a GitHub App.
   GitHub creates apps only on github.com.
2. Set the callback URL to `https://<host>/signin/callback/github`.
3. Under Account permissions, give "Email addresses" read access.
4. Copy the client ID and generate a client secret.

## Configure

```json
"GitHub": { "ClientId": "Iv23li...", "ClientSecret": "<secret>" }
```

## Notes

- A user is keyed on the numeric GitHub user `id`, which never changes. A login (username) can
  change and be claimed by someone else, so it is never used.
- The email is the entry from `GET /user/emails` that is both primary and verified. A user with none
  can link GitHub but cannot sign up with it.
- IAM sends PKCE (S256) and `state`. GitHub offers no forced re-entry of credentials.
