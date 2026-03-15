# Preface
List of features that may be nice to implement

### Soft Deletes
- [ ] Add IsDeleted bool to entities and groups
- [ ] Resepect IsDeleted flag in queries and operations

### Permission ValidFrom / ValidTo
- [ ] Add ValidFrom and ValidTo DateTime to permissions
  - This should be non-nullable and default to DateTime.MinValue and DateTime.MaxValue
- [ ] Resepect ValidFrom and ValidTo in permission checks

### Add Restritions
This is the opposite of permissions, i.e. deny access even if permission exists.
It may also be more complex because there could be different types of restrictions.
Examples of restrictions could be:
- Time-based restrictions (e.g., access denied during certain hours)
- Location-based restrictions (e.g., access denied from certain IP ranges)

- It may be best to implement different types of restrictions as different entities and aggregate them in the IAuthorizationProvider

### Add ITelemetry
Add an ITelemetry interface with HandleTelemetry(TelemetryEvent).
The idea is for consumers of the library to be able to plug in their own telemetry/logging system.
There could be more than one telemetry registered at the consumer level using the decorator pattern.
Need to figure where / what kinds of telemetry make sense, and if different kinds of telemetry events / handlers are needed

### Add IAuditing
When authorization passes and an action is performed, audit who did the action and save it to an audits database

### Add support for managing other users
An account owner has abilities to manage other user's access to the accounts, but not the actual users.
It could be beneficial for an account owner to have access to create and manage users as well.

### Signed Key Auth (Service Auth)
Opt-in auth mechanism for service-to-service communication, following the same pattern as MFA and Google sign-in.
Primarily useful when IAM is hosted behind an API and consumed by external services (e.g., function apps via a thin API client).

- [ ] Add "enable service auth" flow on a user (like enabling MFA)
- [ ] Generate an asymmetric key pair on opt-in; display the private key once, store only the public key
- [ ] Add a new sign-in flow (e.g., `SignInWithSignedToken`) that accepts a client-signed JWT
- [ ] Validate the client JWT signature against the stored public key, enforce short expiry and replay protection
- [ ] Issue a standard IAM JWT on success — everything downstream (RBAC, authorization decorators) works unchanged
- [ ] Support key rotation (disable + re-enable, or multiple active public keys with a grace period)
