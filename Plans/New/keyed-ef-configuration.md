# Keyed EF configuration per library

## Starting cold

For a session picking this up with no history. The change spans three repositories, done in order:

| Repository | Path |
|---|---|
| Corely.IAM | `C:\source\git\ultrabstrong\Corely.IAM` |
| Corely.Billing | `C:\source\git\ultrabstrong\Corely.Billing` |
| DocsToData (consumer) | `C:\source\git\pinnacleinnovation\DocsToData` |

Read each repository's `CLAUDE.md` before touching it. DocsToData's rules are strict: no code
comments at all, and push only when the owner asks. Releasing a library is tagging `vX.Y.Z` on
`master`; ask the owner before tagging, since a published version cannot be deleted.

**Decided with the owner; do not reopen:** the key is a `const string` per library, no GUID. The host
API does not change.

## The problem

`AddIAMServices` and `AddBillingServices` each register the host's factory as a plain
`IEFConfiguration`:

```csharp
serviceCollection.AddScoped(options.EFConfigurationFactory);   // IAM line 47, Billing line 40
```

and each library's DbContext takes an unkeyed `IEFConfiguration`. In a container holding both
libraries, the last registration wins for **both** DbContexts. Harmless while a host points both at
one database; silently wrong the day it points them at two.

It has a second effect, found in DocsToData: its own `ExtractionDbContext` and
`DocumentWorkflowDbContext` also take an unkeyed `IEFConfiguration`, and none of its hosts registers
one. They have been running on whichever library's registration came last.

## The change

Each library registers its factory under its own key and its DbContext asks for that key, the pattern
Corely.DataAccess's demo already shows (`Corely.DataAccess.Demo/DemoDbContexts.cs`,
`ContextConfigurationKeys`).

```csharp
internal static class EFConfigurationKeys
{
    public const string IAM = "Corely.IAM.EFConfiguration";
}

serviceCollection.AddKeyedScoped(EFConfigurationKeys.IAM, (sp, _) => options.EFConfigurationFactory(sp));

internal class IamDbContext([FromKeyedServices(EFConfigurationKeys.IAM)] IEFConfiguration efConfiguration)
```

Billing: `"Corely.Billing.EFConfiguration"` in `Corely.Billing/DataAccess/`, applied to both
`BillingDbContext` constructors. The design-time factories in the migrations projects construct the
context directly and are unaffected. Corely.DataAccess's `EFContextResolver` resolves DbContexts, not
`IEFConfiguration`, so it needs no change; confirm with its tests.

The library no longer registers a plain `IEFConfiguration` at all. That is the point, and it is also
the breaking part: a host that resolved `IEFConfiguration` itself, or built its own DbContext on top
of the library's registration, now gets nothing.

## Work

**Corely.IAM**
1. The key constant, the keyed registration in `ServiceRegistrationExtensions.AddIAMServices`, and
   `[FromKeyedServices]` on both `IamDbContext` constructors.
2. Tests: `AddIAMServices_WithEF_RegistersIEFConfiguration` becomes a keyed assertion, plus a test
   that a host's own unkeyed `IEFConfiguration` is not picked up by `IamDbContext`.
3. Test hosts that swap the configuration by type must swap the keyed one:
   `Corely.IAM.Web.FunctionalTests/Infrastructure/IamWebApplicationFactory.cs` and
   `Demos/DemoAppFactory.cs` (`RemoveAll<IEFConfiguration>()` then `AddSingleton`/`AddScoped`).
   Search the whole repository for `IEFConfiguration` for any others: the ConsoleTest, DevTools and
   WebApp hosts pass factories through `IAMOptions` and should need nothing.
4. Docs: `iam-options.md` and `step-by-step-setup.md` gain one sentence: the factory is private to
   IAM; a host's own DbContexts register their own configuration.
5. Version: this changes what the container exposes. Ask the owner whether it is a major (3.0.0) or a
   minor with a release note. `Corely.IAM` is at 2.2.2; `Corely.IAM.Web` may need a matching bump if
   its tests or package depend on the change.

**Corely.Billing** — the same five steps. Billing is a preview, so the next preview number.

**DocsToData**, after both packages are published
1. Bump `Corely.IAM` and both `Corely.Billing` entries in `Directory.Packages.props`.
2. Register DocsToData's own configuration in each host, from the same factory it already passes
   to the libraries: `services.AddScoped<IEFConfiguration>(CreateEFConfiguration)` in
   `DocsToData.AdminPortalWebApp/Program.cs` and `DocsToData.ConsoleApp/ServiceFactory.cs`.
   `DocsToData.Functions/ServiceFactory.cs` builds its `MsSqlConfiguration` inline in two lambdas,
   and registers IAM only when the system key is set; extract one local factory and register it
   unconditionally, since the Extraction and DocumentWorkflow contexts need it either way.
   `DocsToData.Tests.Core/ServiceFactory.cs` already registers its own.
3. Full suite, including the Functions host tier and the admin portal functional tier, since they are
   the ones that build the real container.

## Done when

Each library's DbContext resolves only its own keyed configuration, proven by a test that registers a
decoy unkeyed configuration; both libraries are released; DocsToData registers its own configuration
and its full suite is green.
