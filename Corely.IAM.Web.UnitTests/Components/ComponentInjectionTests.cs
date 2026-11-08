using System.Reflection;
using Corely.DataAccess.EntityFramework.Configurations;
using Corely.IAM.Security.Providers;
using Corely.IAM.Services;
using Corely.IAM.Web.Components;
using Corely.IAM.Web.Extensions;
using Corely.IAM.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Corely.IAM.Web.UnitTests.Components;

public class ComponentInjectionTests
{
    // Shared across the tab on purpose, and never reaching the database themselves.
    private static readonly Type[] _sharedAcrossTheTab =
    [
        typeof(IAccountDisplayState),
        typeof(IOperationScopes),
        typeof(NavigationManager),
        typeof(IJSRuntime),
    ];

    private static readonly IServiceCollection _services = CreateAppServices();

    private static IServiceCollection CreateAppServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRazorComponents().AddInteractiveServerComponents();
        services.AddIAMWeb();
        services.AddIAMWebBlazor();
        services.AddIAMServices(
            IAMOptions.Create(
                new ConfigurationManager(),
                Mock.Of<ISecurityConfigurationProvider>(),
                _ => Mock.Of<IEFConfiguration>()
            )
        );
        return services;
    }

    private static IEnumerable<Type> Components(IEnumerable<Type> types) =>
        types.Where(t => typeof(IComponent).IsAssignableFrom(t));

    private static List<string> FindScopedInjections(IEnumerable<Type> types) =>
        Components(types)
            .SelectMany(t =>
                t.GetProperties(
                        BindingFlags.Instance
                            | BindingFlags.Public
                            | BindingFlags.NonPublic
                            | BindingFlags.DeclaredOnly
                    )
                    .Where(p => p.GetCustomAttribute<InjectAttribute>() != null)
                    .Where(p => !_sharedAcrossTheTab.Contains(p.PropertyType))
                    .Where(p => LifetimeOf(p.PropertyType) == ServiceLifetime.Scoped)
                    .Select(p => $"{t.Name}.{p.Name} ({p.PropertyType.Name})")
            )
            .ToList();

    private static ServiceLifetime? LifetimeOf(Type serviceType) =>
        _services
            .LastOrDefault(d =>
                d.ServiceType == serviceType
                || (
                    serviceType.IsGenericType
                    && d.ServiceType == serviceType.GetGenericTypeDefinition()
                )
            )
            ?.Lifetime;

    private sealed class ComponentInjectingAScopedService : ComponentBase
    {
        [Inject]
        private IRetrievalService RetrievalService { get; set; } = null!;
    }

    private static Type[] WebComponents() => typeof(AuthenticatedPageBase).Assembly.GetTypes();

    [Fact]
    public void Components_InjectNoScopedServices_OutsideTheTabSharedAllowList()
    {
        var types = WebComponents();

        Assert.Contains(types, t => typeof(OperationComponentBase).IsAssignableFrom(t));
        Assert.Empty(FindScopedInjections(types));
    }

    [Fact]
    public void Components_OwnNoScopeForTheirLifetime_ForEveryComponent()
    {
        var owning = Components(WebComponents())
            .Where(t => typeof(OwningComponentBase).IsAssignableFrom(t))
            .Select(t => t.Name);

        Assert.Empty(owning);
    }

    [Fact]
    public void FindScopedInjections_ReportsAScopedService_ForAnyComponent()
    {
        var found = FindScopedInjections([typeof(ComponentInjectingAScopedService)]);

        Assert.Single(found);
    }
}
