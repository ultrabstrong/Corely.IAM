# Result Codes

Complete reference of all result code enums across Corely.IAM.

## Common Result Codes

### RetrieveResultCode

| Code | Meaning |
|------|---------|
| `Success` | Entity found |
| `NotFoundError` | Entity not found |
| `UnauthorizedError` | Insufficient permissions |

### ModifyResultCode

| Code | Meaning |
|------|---------|
| `Success` | Update applied |
| `NotFoundError` | Entity not found |
| `UnauthorizedError` | Insufficient permissions |
| `SystemDefinedError` | Cannot modify system-defined entity |
| `ValidationError` | Input validation failed |
| `UsernameExistsError` | Username already taken |
| `EmailExistsError` | Email already in use |

## Account Result Codes

### CreateAccountResultCode

| Code | Meaning |
|------|---------|
| `Success` | Account created |
| `AccountExistsError` | Account name already exists |
| `UserOwnerNotFoundError` | Owner user not found |
| `ValidationError` | Input validation failed |

### DeleteAccountResultCode

| Code | Meaning |
|------|---------|
| `Success` | Account deleted |
| `AccountNotFoundError` | Account not found |
| `UnauthorizedError` | Insufficient permissions |

## User Result Codes

### CreateUserResultCode

| Code | Meaning |
|------|---------|
| `Success` | User created |
| `UserExistsError` | Username already taken |
| `ValidationError` | Input validation failed |

### DeleteUserResultCode

| Code | Meaning |
|------|---------|
| `Success` | User deleted |
| `UserNotFoundError` | User not found |
| `UserIsSoleAccountOwnerError` | Cannot delete sole owner |
| `UnauthorizedError` | Insufficient permissions |

## Group Result Codes

### CreateGroupResultCode

| Code | Meaning |
|------|---------|
| `Success` | Group created |
| `GroupExistsError` | Group name exists in account |
| `AccountNotFoundError` | Account not found |
| `UnauthorizedError` | Insufficient permissions |
| `ValidationError` | Input validation failed |

### DeleteGroupResultCode

| Code | Meaning |
|------|---------|
| `Success` | Group deleted |
| `GroupNotFoundError` | Group not found |
| `GroupHasSoleOwnersError` | Contains sole account owner |
| `UnauthorizedError` | Insufficient permissions |

## Role Result Codes

### CreateRoleResultCode

| Code | Meaning |
|------|---------|
| `Success` | Role created |
| `RoleExistsError` | Role name exists in account |
| `AccountNotFoundError` | Account not found |
| `UnauthorizedError` | Insufficient permissions |
| `ValidationError` | Input validation failed |

### DeleteRoleResultCode

| Code | Meaning |
|------|---------|
| `Success` | Role deleted |
| `RoleNotFoundError` | Role not found |
| `SystemDefinedRoleError` | Cannot delete system role |
| `UnauthorizedError` | Insufficient permissions |

## Permission Result Codes

### CreatePermissionResultCode

| Code | Meaning |
|------|---------|
| `Success` | Permission created |
| `PermissionExistsError` | Duplicate resource type + ID in account |
| `AccountNotFoundError` | Account not found |
| `UnauthorizedError` | Insufficient permissions |
| `ValidationError` | Invalid resource type or no CRUDX flags |

### DeletePermissionResultCode

| Code | Meaning |
|------|---------|
| `Success` | Permission deleted |
| `PermissionNotFoundError` | Permission not found |
| `SystemDefinedPermissionError` | Cannot delete system permission |
| `UnauthorizedError` | Insufficient permissions |

## BasicAuth Result Codes

### CreateBasicAuthResultCode

| Code | Meaning |
|------|---------|
| `Success` | Credentials created |
| `BasicAuthExistsError` | User already has credentials |
| `PasswordValidationError` | Password doesn't meet requirements |
| `ValidationError` | Input validation failed |

### VerifyBasicAuthResultCode

| Code | Meaning |
|------|---------|
| `Success` | Verification complete |
| `UserNotFoundError` | User not found |
| `UnauthorizedError` | Insufficient permissions |

## Authentication Result Codes

### SignInResultCode

| Code | Meaning |
|------|---------|
| `Success` | Authentication succeeded |
| `UserNotFoundError` | Username not found |
| `UserLockedError` | Account locked (too many failures) |
| `PasswordMismatchError` | Incorrect password |
| `SignatureKeyNotFoundError` | User signature key missing |
| `AccountNotFoundError` | Target account not found |
| `InvalidAuthTokenError` | Token generation failed |
| `MfaRequiredChallenge` | TOTP is enabled — MFA challenge issued |
| `InvalidMfaCodeError` | TOTP or recovery code is invalid |
| `MfaChallengeExpiredError` | MFA challenge expired or already used |
| `InvalidGoogleTokenError` | Google ID token validation failed |
| `GoogleAuthNotLinkedError` | No user linked to this Google account |

### UserAuthTokenValidationResultCode

| Code | Meaning |
|------|---------|
| `Success` | Token valid |
| `InvalidTokenError` | JWT format invalid |
| `TokenNotFoundError` | Token not in database |
| `TokenRevokedError` | Token revoked |
| `TokenExpiredError` | Token expired |
| `UserNotFoundError` | User from token not found |
| `MissingDeviceIdClaim` | Device ID claim missing |
| `SignatureValidationError` | JWT signature invalid |

## Invitation Result Codes

### CreateInvitationResultCode

| Code | Meaning |
|------|---------|
| `Success` | Invitation created |
| `AccountNotFoundError` | Account not found |
| `ValidationError` | Invalid email or parameters |
| `UnauthorizedError` | Insufficient permissions |
| `UserAlreadyInAccountError` | User already in account |

### AcceptInvitationResultCode

| Code | Meaning |
|------|---------|
| `Success` | Invitation accepted, user added to account |
| `InvitationNotFoundError` | Invalid token |
| `InvitationExpiredError` | Token expired |
| `InvitationRevokedError` | Token revoked |
| `InvitationAlreadyAcceptedError` | Already used |
| `AddToAccountError` | Failed to add user to account |
| `EmailMismatchError` | Email doesn't match authenticated user |
| `UnauthorizedError` | Insufficient permissions |

## TOTP Result Codes

### EnableTotpResultCode

| Code | Meaning |
|------|---------|
| `Success` | TOTP setup initiated, secret and recovery codes returned |
| `AlreadyEnabledError` | TOTP is already enabled for this user |

### ConfirmTotpResultCode

| Code | Meaning |
|------|---------|
| `Success` | TOTP confirmed and enabled |
| `NotFoundError` | No TOTP setup found |
| `AlreadyEnabledError` | TOTP is already confirmed |
| `InvalidCodeError` | TOTP code does not match |

### DisableTotpResultCode

| Code | Meaning |
|------|---------|
| `Success` | TOTP disabled and removed |
| `NotFoundError` | TOTP is not enabled |
| `InvalidCodeError` | TOTP code does not match |

### TotpStatusResultCode

| Code | Meaning |
|------|---------|
| `Success` | Status retrieved |

### RegenerateTotpRecoveryCodesResultCode

| Code | Meaning |
|------|---------|
| `Success` | New recovery codes generated |
| `NotFoundError` | No TOTP setup found |
| `NotEnabledError` | TOTP is not yet enabled |

### VerifyTotpOrRecoveryCodeResultCode

| Code | Meaning |
|------|---------|
| `TotpCodeValid` | TOTP code verified |
| `RecoveryCodeValid` | Recovery code verified (now consumed) |
| `NotFoundError` | TOTP is not enabled |
| `InvalidCodeError` | Neither TOTP nor recovery code matched |

## Google Auth Result Codes

### LinkGoogleAuthResultCode

| Code | Meaning |
|------|---------|
| `Success` | Google account linked |
| `InvalidGoogleTokenError` | Google ID token validation failed |
| `AlreadyLinkedError` | User already has a linked Google account |
| `GoogleAccountInUseError` | This Google account is linked to another user |

### UnlinkGoogleAuthResultCode

| Code | Meaning |
|------|---------|
| `Success` | Google account unlinked |
| `NotLinkedError` | No Google account linked |
| `LastAuthMethodError` | Cannot unlink the only authentication method |

### AuthMethodsResultCode

| Code | Meaning |
|------|---------|
| `Success` | Auth methods retrieved |

### RegisterUserWithGoogleResultCode

| Code | Meaning |
|------|---------|
| `Success` | User created from Google account |
| `InvalidGoogleTokenError` | Google ID token validation failed |
| `GoogleAccountInUseError` | This Google account is linked to another user |
| `UserExistsError` | Username derived from Google email already taken |
| `ValidationError` | Input validation failed |

## BasicAuth Result Codes

### DeregisterBasicAuthResultCode

| Code | Meaning |
|------|---------|
| `Success` | Password authentication removed |
| `NotFoundError` | User has no basic auth credentials |
| `LastAuthMethodError` | Cannot remove the only authentication method |
| `UnauthorizedError` | Insufficient permissions |
