# Amazon

OAuth 2.0 through Login with Amazon, with a client secret.

## Register

1. In the Amazon Developer Console, create a security profile and enable it for Login with Amazon.
2. Under Web Settings, add `https://<host>/signin/callback/amazon` to Allowed Return URLs.
3. Copy the client ID and client secret.

## Configure

```json
"Amazon": { "ClientId": "amzn1.application-oa2-client...", "ClientSecret": "<secret>" }
```

## Notes

- A user is keyed on the profile's `user_id`, which is the same for every app of one company.
- Amazon confirms a customer's email when the account is created, so a returned email counts as
  verified.
- IAM sends PKCE (S256) and `state`, and reads the profile with the `profile` scope.
- Amazon offers no forced re-entry of credentials.
