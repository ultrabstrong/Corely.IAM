# Facebook

OAuth 2.0 through Facebook Login, with the app secret.

## Register

1. In Meta for Developers, create an app of type Consumer and add Facebook Login.
2. Add `https://<host>/signin/callback/facebook` to the valid OAuth redirect URIs. In Development
   mode localhost redirects are allowed and only people with a role on the app can sign in.
3. Switch the `email` permission to Advanced Access and take the app Live before people without a
   role sign in. Meta may ask for Business Verification for Advanced Access.
4. Copy the app ID and app secret.

## Configure

```json
"Facebook": { "ClientId": "<app id>", "ClientSecret": "<app secret>" }
```

## Notes

- IAM checks every access token with `debug_token`: it must be valid and issued to this app, for the
  user it reads.
- Graph calls carry `appsecret_proof`.
- Facebook returns an email only once it is confirmed, so a returned email counts as verified. An
  account registered with a phone number has none, and can be linked but cannot sign up.
- The Graph API version is fixed in IAM and moves with Meta's schedule.
- Facebook offers no forced re-entry of credentials.
