# Plan: Host platform settings

**Status: idea.** Problem and use cases only. How to build it is for a later discussion.

## Problem Statement

The platform settings belong to IAM alone. The platform account holds a fixed set of them (audit
and security), and IAM.Web shows them on its own pages under `/platform-settings`. A host has no way
to add settings of its own there.

A host often has decisions that are platform wide in the same sense: made by the people who run the
application, applying to every account, and not something a customer should change. Today such a
host either puts them in configuration, so changing one means a deploy, or builds its own storage,
page and access rules, which then sit apart from the platform settings a platform admin already
knows.

Some pieces exist. A host registers its own resource types, and permissions held in the platform
account apply in every account, so "only a platform admin may change this" can already be
expressed. What is missing is a recognised place for host settings to live and be managed beside
IAM's own.

## Use cases

From DocsToData, the first host to ask for it:

1. **Switch a capability off for everyone.** Turn off Textract across the platform, for example
   during a provider outage or while its cost is reviewed, without a deploy.
2. **Choose what is allowed.** Set the extraction models accounts may use at the platform level.
3. **Turn something on for selected accounts only.** Make a capability available to a few accounts
   first, as a development or early access mode, before opening it to everyone.

The third differs from the first two. It is a setting with a per account answer, closer to a
feature flag than to a single platform value.

## What a solution should give

- A platform admin manages host settings in the same place, and with the same kind of access
  control, as IAM's platform settings.
- Only someone holding the right permission in the platform account can change them.
- The host decides what its settings are, and keeps the meaning of each one.
- Changes reach running instances without a restart, as security settings already do.

## Open questions

- Does IAM store host settings, or only give them a place in the portal and the access rules, with
  each host storing its own?
- Is "on for selected accounts" part of this, or a separate feature flag concern?
- Could Corely.Billing's grants, which are already per account and provisioned by platform
  members, cover the per account case?
- Are host settings audited the way IAM's platform settings are?
