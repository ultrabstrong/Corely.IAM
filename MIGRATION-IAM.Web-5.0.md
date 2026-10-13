# Migrating to Corely.IAM.Web 5.0

One change needs action: `UseIAMWebAuthentication()` no longer sets security headers. A host that
upgrades without setting its own serves pages with none of them, and nothing fails to tell it so.

## Security headers are the host's

These were set on every response and now are not:

- `X-Frame-Options`, `X-Content-Type-Options`, `Referrer-Policy`, `Permissions-Policy`,
  `Cross-Origin-Opener-Policy`, `Cross-Origin-Resource-Policy`, `X-Permitted-Cross-Domain-Policies`
- `Cache-Control: no-store, no-cache, must-revalidate` and `Pragma: no-cache` on pages
- `Strict-Transport-Security: max-age=31536000; includeSubDomains` outside development

Set the ones the app wants before `UseIAMWebAuthentication()`, in the same place as the
Content-Security-Policy. `Corely.IAM.Web/Docs/security.md` has a starting set that reproduces the
old headers, with HSTS through `UseHsts()`, whose default lifetime is 30 days. For the old
year long lifetime:

```csharp
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
});
```

Static files no longer get `Cache-Control: public, max-age=86400`. Serve them with
`MapStaticAssets()`, which sets cache headers from each file's content.
