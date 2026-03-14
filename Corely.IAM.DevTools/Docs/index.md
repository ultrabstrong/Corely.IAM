# Corely.IAM.DevTools

Command-line developer toolkit for cryptographic operations and direct IAM service interaction. Built on `System.CommandLine` with auto-discovered commands.

All commands support `--help` for full argument and option details.

## Command Groups

| Group | Purpose |
|-------|---------|
| `sym-encrypt` | Symmetric encryption — create keys, encrypt/decrypt values |
| `asym-encrypt` | Asymmetric encryption — create key pairs, encrypt/decrypt values |
| `sym-sign` | Symmetric signatures — create keys, sign/verify messages |
| `asym-sign` | Asymmetric signatures — create key pairs, sign/verify messages |
| `shash` | Salted hashing — list providers, hash values |
| `base64` | Base64 encode/decode |
| `url` | URL encode/decode |
| `config` | Local settings file management (provider, connection string, system key) |
| `provider` | Database provider selection |
| `auth` | Authentication — sign in, sign out, switch accounts, MFA, Google |
| `totp` | TOTP operations — enable, confirm, disable, status, code generation |
| `google` | Google auth — link, unlink, status |
| `register` | Entity registration — users, accounts, groups, roles, permissions |
| `retrieval` | Entity retrieval — list/get with pagination, hydration, key providers |
| `deregister` | Entity deregistration — remove entities and relationships |
| `modify` | Entity modification — update users, accounts, groups, roles |
| `invitation` | Invitation management — create, accept, revoke, list |

## Setup

DevTools requires a local settings file for commands that interact with the database.

### 1) Initialize Settings

```bash
dotnet run -- config init -p mssql -c "Server=(localdb)\MSSQLLocalDB;Database=CorelIAM;Trusted_Connection=True;"
```

Options:
- `-p, --provider` — database provider (`MySql`, `MariaDb`, `MsSql`)
- `-c, --connection` — connection string
- `-f, --force` — overwrite existing settings file

### 2) Set the System Key

Generate a new key and save it to settings:

```bash
dotnet run -- config system-key --generate
```

Or set an existing key:

```bash
dotnet run -- config system-key --key "your-hex-key"
```

### 3) Verify Connection

```bash
dotnet run -- config test-connection
```

### 4) View Current Settings

```bash
dotnet run -- config show
```

## Crypto Commands

Standalone cryptographic operations that do not require a database connection. Useful for key generation, testing encryption round-trips, and verifying signatures.

### Symmetric Encryption

```bash
# Create a new key
dotnet run -- sym-encrypt --create

# List available providers
dotnet run -- sym-encrypt --list

# Encrypt a value with a key
dotnet run -- sym-encrypt "your-hex-key" -e "plaintext"

# Decrypt a value with a key
dotnet run -- sym-encrypt "your-hex-key" -d "encrypted-value"

# Validate a key
dotnet run -- sym-encrypt "your-hex-key" --validate
```

Default provider: `AES-256-CBC-PKCS7`. Pass an alternative as the second argument.

### Asymmetric Encryption

```bash
# Create a new key pair
dotnet run -- asym-encrypt --create

# Encrypt/decrypt using a key file (format: public\nprivate)
dotnet run -- asym-encrypt "keyfile.txt" -e "plaintext"
dotnet run -- asym-encrypt "keyfile.txt" -d "encrypted-value"
```

Default provider: `RSA-2048-OAEP-SHA256`.

### Symmetric Signatures

```bash
# Create a new signing key
dotnet run -- sym-sign --create

# Sign a message
dotnet run -- sym-sign "your-hex-key" "message to sign"

# Verify a signature
dotnet run -- sym-sign "your-hex-key" "message to sign" -s "signature"
```

Default provider: `HMAC-SHA256`.

### Asymmetric Signatures

```bash
# Create a new key pair
dotnet run -- asym-sign --create

# Sign a message using a key file
dotnet run -- asym-sign "keyfile.txt" "message to sign"

# Verify a signature
dotnet run -- asym-sign "keyfile.txt" "message to sign" -s "signature"
```

Default provider: `ECDSA-P256-SHA256`.

### Hashing

```bash
# List available hash providers
dotnet run -- shash

# Hash a value
dotnet run -- shash "Salted-SHA256" "value to hash"
```

### Encoding Utilities

```bash
# Base64
dotnet run -- base64 -e "value"
dotnet run -- base64 -d "encoded-value"

# URL encoding
dotnet run -- url -e "value with spaces"
dotnet run -- url -d "value%20with%20spaces"
```

## IAM Service Commands

Commands that interact with the database through the full IAM service stack. Require a configured settings file (see Setup above) and — for most operations — an authenticated session.

### Authentication

Sign in to establish a session. The auth token is saved to a local file for subsequent commands.

```bash
# Sign in (prompts for or accepts request JSON)
dotnet run -- auth signin "request.json"

# Switch to a different account
dotnet run -- auth switch-account

# Sign out current session
dotnet run -- auth signout

# Sign out all sessions
dotnet run -- auth signout-all

# Sign in with Google ID token
dotnet run -- auth signin-google "google-id-token.txt"

# Complete MFA verification
dotnet run -- auth verify-mfa "challenge-token" "123456"
```

### TOTP (Multi-Factor Authentication)

Manage TOTP-based multi-factor authentication. Most commands require an authenticated session.

```bash
# Enable TOTP — outputs secret, setup URI, and recovery codes
dotnet run -- totp enable

# Confirm TOTP setup with a code from your authenticator app
dotnet run -- totp confirm "123456"

# Check TOTP status
dotnet run -- totp status

# Regenerate recovery codes
dotnet run -- totp regenerate-codes

# Disable TOTP (requires a valid code)
dotnet run -- totp disable "123456"

# Standalone: generate a TOTP code from a secret (no auth needed)
dotnet run -- totp generate-code "JBSWY3DPEHPK3PXP"

# Standalone: validate a TOTP code against a secret (no auth needed)
dotnet run -- totp validate-code "JBSWY3DPEHPK3PXP" "123456"
```

### Google Authentication

Manage Google account linking. Requires an authenticated session.

```bash
# Link a Google account (reads ID token from file)
dotnet run -- google link "google-id-token.txt"

# Check linked Google account status
dotnet run -- google status

# Unlink Google account
dotnet run -- google unlink
```

### Registration

Create entities. Each subcommand accepts a JSON request file. Use `--create` to generate a template.

```bash
# Generate a request template
dotnet run -- register user --create

# Register from a JSON file
dotnet run -- register user "create-user-request.json"
```

| Subcommand | Description |
|------------|-------------|
| `user` | Register a new user |
| `account` | Register a new account |
| `group` | Register a group in the current account |
| `role` | Register a role in the current account |
| `permission` | Register a permission in the current account |
| `permissions-with-role` | Register permissions and assign to a role |
| `user-with-google` | Register a new user from a Google ID token |
| `user-with-account` | Register a user and create their account |
| `users-with-group` | Register users into a group |
| `roles-with-user` | Assign roles to a user |
| `roles-with-group` | Assign roles to a group |

### Retrieval

Query entities with pagination and optional hydration of related data.

```bash
# List users (default: skip=0, take=25)
dotnet run -- retrieval list-users --skip 0 --take 10

# Get a single entity with related data
dotnet run -- retrieval get-user "guid" --hydrate
```

| Subcommand | Description |
|------------|-------------|
| `list-users` | List users with pagination |
| `list-accounts` | List accounts with pagination |
| `list-groups` | List groups with pagination |
| `list-roles` | List roles with pagination |
| `list-permissions` | List permissions with pagination |
| `list-resource-types` | List registered resource types |
| `get-user` | Get user by ID |
| `get-account` | Get account by ID |
| `get-group` | Get group by ID |
| `get-role` | Get role by ID |
| `get-permission` | Get permission by ID |

Key provider subcommands for account/user-scoped cryptographic operations:

| Subcommand | Description |
|------------|-------------|
| `account-sym-encrypt` | Account symmetric encryption (`--encrypt`, `--decrypt`, `--reencrypt`) |
| `account-asym-encrypt` | Account asymmetric encryption |
| `account-asym-sign` | Account asymmetric signing |
| `user-sym-encrypt` | User symmetric encryption |
| `user-asym-encrypt` | User asymmetric encryption |
| `user-asym-sign` | User asymmetric signing |

### Deregistration

Remove entities and relationships.

```bash
dotnet run -- deregister user "guid"
dotnet run -- deregister user-from-account "request.json"
```

| Subcommand | Description |
|------------|-------------|
| `user` | Delete a user |
| `account` | Delete an account |
| `group` | Delete a group |
| `role` | Delete a role |
| `permission` | Delete a permission |
| `user-from-account` | Remove a user from an account |
| `users-from-group` | Remove users from a group |
| `permissions-from-role` | Remove permissions from a role |
| `roles-from-user` | Unassign roles from a user |
| `roles-from-group` | Unassign roles from a group |
| `basic-auth` | Remove password authentication |

### Modification

Update entity properties. Each subcommand accepts a JSON request file.

```bash
dotnet run -- modify user "update-user-request.json"
```

| Subcommand | Description |
|------------|-------------|
| `user` | Update user properties |
| `account` | Update account properties |
| `group` | Update group properties |
| `role` | Update role properties |

### Invitations

Manage account invitations.

```bash
# Create an invitation
dotnet run -- invitation create --account-id "guid" --email "user@example.com" --expires 7

# Accept an invitation
dotnet run -- invitation accept --token "invitation-token"

# Revoke an invitation
dotnet run -- invitation revoke "invitation-id"

# List invitations
dotnet run -- invitation list --skip 0 --take 25
```

## Notes

- All commands support `--help` for full argument and option details
- Crypto commands work standalone — no database required
- IAM service commands require `config init` + `auth signin` first
- JSON request files can be generated with the `--create` flag on registration commands
- Key file format for asymmetric operations: public key on line 1, private key on line 2
