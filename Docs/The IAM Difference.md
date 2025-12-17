# Corely.IAM

**Stop letting authentication be the reason you don't build the thing.**

Corely.IAM is a host-agnostic, multi-tenant identity and access management 
library for .NET. It gives you users, accounts, roles, permissions, and 
JWT authentication out of the box—so you can focus on your actual product.

## Why?

Because setting up auth *again* shouldn't take a week. Because "we'll do 
it properly later" never happens. Because your side project deserves the 
same IAM foundation as enterprise software.

## Features

- **Host-agnostic**: Works in ASP.NET, console apps, desktop, mobile—anywhere
- **Multi-tenant native**: Account-scoped everything, from day one
- **Resource-level permissions**: CRUDX on specific resources, not just roles
- **Algorithm-agile crypto**: Swap encryption/hashing providers without code changes
- **No external dependencies**: No Auth0, no Keycloak, no cloud lock-in

## Why not ASP.NET Core Identity?

Corely.IAM is designed to be **host-agnostic**. The same authentication and 
authorization logic works identically in:
- ASP.NET Core Web APIs
- Console applications  
- Desktop apps (WPF, WinForms, MAUI)
- Mobile apps
- Background services

ASP.NET Core Identity is tightly coupled to the HTTP request pipeline and 
`HttpContext`, making it unsuitable for these scenarios without significant 
modification.

Additionally, Corely.IAM provides:
- Native multi-tenancy (Account-scoped resources)
- Resource-level CRUDX permissions (finer than claims/policies)
- Algorithm-agile cryptography via Corely.Security
- Per-user/account key management