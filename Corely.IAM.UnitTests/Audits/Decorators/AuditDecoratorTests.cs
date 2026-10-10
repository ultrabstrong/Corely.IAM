using System.Reflection;
using AutoFixture;
using AutoFixture.Kernel;
using Corely.Common.Filtering;
using Corely.Common.Filtering.Ordering;
using Corely.IAM.Audits.Providers;
using Corely.IAM.Services;

namespace Corely.IAM.UnitTests.Audits.Decorators;

public class AuditDecoratorTests
{
    private static readonly HashSet<string> _passThroughMethods =
    [
        nameof(IAuthenticationService.AuthenticateWithTokenAsync),
        nameof(IAuthenticationService.AuthenticateAsSystem),
    ];

    public static TheoryData<Type, string> DecoratedMethods()
    {
        var data = new TheoryData<Type, string>();
        foreach (var decorator in AuditDecorators())
        {
            var serviceInterface = ServiceInterfaceOf(decorator);
            foreach (var method in serviceInterface.GetMethods())
            {
                data.Add(decorator, method.Name);
            }
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(DecoratedMethods))]
    public async Task EveryMethod_CallsTheInnerServiceOnce_AndRecordsItUnderItsOwnName(
        Type decoratorType,
        string methodName
    )
    {
        var serviceInterface = ServiceInterfaceOf(decoratorType);
        var inner = (Mock)
            Activator.CreateInstance(typeof(Mock<>).MakeGenericType(serviceInterface))!;
        var auditProvider = new CapturingAuditProvider();
        var decorator = Activator.CreateInstance(decoratorType, inner.Object, auditProvider)!;
        var method = serviceInterface.GetMethods().Single(m => m.Name == methodName);

        var returned = method.Invoke(decorator, CreateArguments(method));
        if (returned is Task task)
            await task;

        Assert.Single(inner.Invocations, i => i.Method.Name == methodName);
        if (_passThroughMethods.Contains(methodName))
        {
            Assert.Empty(auditProvider.Calls);
            return;
        }

        var captured = Assert.Single(auditProvider.Calls);
        Assert.Equal(serviceInterface.Name, captured.Call.Service);
        Assert.Equal(methodName, captured.Operation);
        Assert.False(string.IsNullOrWhiteSpace(captured.Call.ResourceType));
    }

    [Fact]
    public void EveryAuditDecorator_DecoratesAServiceInterface()
    {
        Assert.NotEmpty(AuditDecorators());
        Assert.All(AuditDecorators(), d => Assert.NotNull(ServiceInterfaceOf(d)));
    }

    private static IEnumerable<Type> AuditDecorators() =>
        typeof(IRegistrationService)
            .Assembly.GetTypes()
            .Where(t =>
                t.IsClass
                && t.Namespace == typeof(IRegistrationService).Namespace
                && t.Name.EndsWith("AuditDecorator")
            )
            .OrderBy(t => t.Name);

    private static Type ServiceInterfaceOf(Type decorator) =>
        decorator.GetInterfaces().Single(i => i != typeof(IAuditProvider));

    private static object?[] CreateArguments(MethodInfo method)
    {
        var fixture = new Fixture();
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        fixture.Customizations.Add(new NullQueryBuilders());
        var context = new SpecimenContext(fixture);
        return [.. method.GetParameters().Select(p => context.Resolve(p.ParameterType))];
    }

    private sealed class NullQueryBuilders : ISpecimenBuilder
    {
        public object? Create(object request, ISpecimenContext context) =>
            request is Type { IsGenericType: true } type
            && (
                type.GetGenericTypeDefinition() == typeof(FilterBuilder<>)
                || type.GetGenericTypeDefinition() == typeof(OrderBuilder<>)
            )
                ? null
                : new NoSpecimen();
    }
}
