# Resource type presentation

Status: released in Corely.IAM 6.1.0, Corely.IAM.Web 8.1.0 and corely-iam-db 6.1.0.

## What

A resource type has a code name and one free text description, and every list of them shows that
description in registration order. IAM's descriptions are short; Billing's and DocsToData's are a
title and an explanation in one sentence. The lists have no order, no grouping, no difference
between a type's name and what it means, and lines that run off the page.

Each type gains a short title and a group, and one picker shows every list of types the same way.

## Registry

- `ResourceTypeInfo` gains `Title`, defaulting to the name, and `Group`, defaulting to none.
- `IAMOptions.RegisterResourceType(name, title, description, group, params ownerActions)` beside the
  existing overload, which keeps working and registers an untitled, ungrouped type.
- IAM's own types fill both. Groups are constants on `ResourceTypeInfo`:
  `IDENTITY_AND_ACCESS_GROUP` for accounts, users, groups, roles, permissions, API keys and the audit
  log and its settings; `PLATFORM_SETTINGS_GROUP` for the platform settings types, and for a host's.

| Type | Title | Description |
|------|-------|-------------|
| `account` | Accounts | An account and its settings |
| `user` | Users | The account's users and what they belong to |
| `group` | Groups | Groups of users and the roles they carry |
| `role` | Roles | Roles and the permissions they hold |
| `permission` | Permissions | What a permission allows, on which resources |
| `api_key` | API keys | Keys a program signs in with, their secrets and roles |
| `audit` | Audit log | The account's audit entries |
| `audit_settings` | Audit settings | What the account audits and for how long |
| `platform_audit_settings` | Audit | Platform wide audit defaults |
| `platform_security_settings` | Security | Passwords, sign in, two factor and API keys for every account |

## Presentation

- `ResourceTypeInfo` lists group through a Web extension: Identity and access first, then the other
  groups by name, Platform settings last, ungrouped types under Other; types by title within a group.
- `ResourceTypePicker` renders that: the group as a heading, each type as its title, its description
  muted beneath, and its code name small; text wraps. Single choice as radios, several as checkboxes,
  and a search box once the list is long.
- It replaces: the resource type select in Create Permission, the API key resource types in the
  security settings (both views), and the audit log's type filter, as grouped options.
- `PickerItem` gains an optional group, and `EntityPickerModal` heads its items by group. Adding
  permissions to a role groups them by their type's title.

## Consumers

- Corely.Billing.IAM: group Billing, titles Grants, Grant requests, Consumption, Quota and All
  accounts, and descriptions without the title folded in.
- DocsToData: titles and groups for its types, the Permissions menu icon (`bi-patch-check`, as IAM
  uses), and an API keys menu item shown when API keys are on and the user can read them.

## Release

Corely.IAM 6.1.0, Corely.IAM.Web 8.1.0, Corely.Billing.IAM 4.1.0. Proven in DocsToData on local
builds before tagging.

## As built

| Planned | Built |
|---------|-------|
| One picker, list or checkboxes | `ResourceTypePicker` also lays types out as cards (`Cards`), which the security settings use full width below the label; read only it shows only the chosen types |
| Code name beside the title | Below the description, muted, and left out when a type has no title of its own |
| Role permissions grouped by type title | Grouped by `GroupedTitle` ("Platform settings: Audit", "Billing: Grants"; Identity and access types by title alone) in the picker's group order, through `PickerItem.GroupOrder` |
| | `PermissionExtensions` gives a picker's second line: the actions and the scope ("Read, Update on every resource") |
| | The IAM 6 migration gave every platform security row that existed before it an API key lifetime of 0, outside its range, so no key could be created and the settings could not be saved. Migration `ApiKeyLifetimeDefault` sets 0 to 365 |
| | DocsToData's menu and dashboard used the key icon for permissions and had no API keys item; both fixed there |
