using System.Reflection;
using Corely.IAM.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.UnitTests.Audits.Decorators;

public class AuditDecoratorCoverageTests
{
    private const string INNER_FIELD = "_inner";
    private const string AUDIT_DECORATOR_SUFFIX = "AuditDecorator";
    private const string AUTHORIZATION_DECORATOR_SUFFIX = "AuthorizationDecorator";
    private const string TELEMETRY_DECORATOR_SUFFIX = "TelemetryDecorator";

    public static TheoryData<Type> ServiceInterfaces() =>
        [
            .. typeof(IRegistrationService)
                .Assembly.GetExportedTypes()
                .Where(t =>
                    t.IsInterface
                    && t.Namespace == typeof(IRegistrationService).Namespace
                    && t.Name.StartsWith('I')
                    && t.Name.EndsWith("Service")
                )
                .OrderBy(t => t.Name),
        ];

    [Fact]
    public void ServiceInterfaces_AreFound()
    {
        Assert.Contains(typeof(IRegistrationService), ServiceInterfaces().Select(r => r.Data));
        Assert.Contains(typeof(IAuthenticationService), ServiceInterfaces().Select(r => r.Data));
    }

    [Theory]
    [MemberData(nameof(ServiceInterfaces))]
    public void EveryServiceInterface_HasAnAuditDecoratorRegistered(Type serviceInterface)
    {
        var chain = ResolveChain(serviceInterface);

        Assert.Contains(chain, t => t.Name.EndsWith(AUDIT_DECORATOR_SUFFIX));
    }

    [Theory]
    [MemberData(nameof(ServiceInterfaces))]
    public void DecoratorsRun_TelemetryThenAuditThenAuthorizationThenTheService(
        Type serviceInterface
    )
    {
        var chain = ResolveChain(serviceInterface).Select(t => t.Name).ToList();

        var telemetry = chain.FindIndex(n => n.EndsWith(TELEMETRY_DECORATOR_SUFFIX));
        var audit = chain.FindIndex(n => n.EndsWith(AUDIT_DECORATOR_SUFFIX));
        var authorization = chain.FindIndex(n => n.EndsWith(AUTHORIZATION_DECORATOR_SUFFIX));
        var service = chain.Count - 1;

        Assert.True(audit >= 0, $"{serviceInterface.Name} has no audit decorator");
        if (telemetry >= 0)
            Assert.True(telemetry < audit, $"{serviceInterface.Name}: telemetry must wrap audit");
        if (authorization >= 0)
            Assert.True(
                audit < authorization,
                $"{serviceInterface.Name}: audit must wrap authorization"
            );
        Assert.True(audit < service, $"{serviceInterface.Name}: audit must wrap the service");
        Assert.DoesNotContain("Decorator", chain[service]);
    }

    private static List<Type> ResolveChain(Type serviceInterface)
    {
        using var scope = new ServiceFactory()
            .GetRequiredService<IServiceScopeFactory>()
            .CreateScope();
        object? current = scope.ServiceProvider.GetRequiredService(serviceInterface);
        var chain = new List<Type>();
        while (current is not null)
        {
            chain.Add(current.GetType());
            current = current
                .GetType()
                .GetField(INNER_FIELD, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(current);
        }
        return chain;
    }
}
