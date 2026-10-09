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

public class OwningComponentInjectionTests
{
    private static readonly Type[] _sharedAcrossTheTab =
    [
        typeof(IAccountDisplayState),
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

    private static List<string> FindScopedInjections(IEnumerable<Type> types) =>
        types
            .Where(t => typeof(OwningComponentBase).IsAssignableFrom(t))
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

    private sealed class ComponentInjectingAScopedService : OwningComponentBase
    {
        [Inject]
        private IRetrievalService RetrievalService { get; set; } = null!;
    }

    [Fact]
    public void OwningComponents_InjectNoScopedServices_OutsideTheTabSharedAllowList()
    {
        var types = typeof(AuthenticatedPageBase).Assembly.GetTypes();

        Assert.Contains(types, t => typeof(OwningComponentBase).IsAssignableFrom(t));
        Assert.Empty(FindScopedInjections(types));
    }

    [Fact]
    public void FindScopedInjections_ReportsAScopedServiceInjectedIntoAnOwningComponent()
    {
        var found = FindScopedInjections([typeof(ComponentInjectingAScopedService)]);

        Assert.Single(found);
    }
}
