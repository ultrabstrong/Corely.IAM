using System.Reflection;
using System.Runtime.CompilerServices;

namespace Corely.IAM.UnitTests.PublicSurface;

internal static class PublicTypes
{
    private const BindingFlags PUBLIC_MEMBERS =
        BindingFlags.Public
        | BindingFlags.Instance
        | BindingFlags.Static
        | BindingFlags.DeclaredOnly;

    private static readonly string[] _internalNamespaceSegments =
    [
        "Entities",
        "Processors",
        "Mappers",
        "Repos",
        "Validators",
    ];

    private static readonly string[] _internalNameSuffixes = ["Entity", "Processor"];

    public static IReadOnlyList<Type> ExportedTypes(Assembly assembly) =>
        [
            .. assembly
                .GetExportedTypes()
                .Where(t =>
                    !t.IsDefined(typeof(CompilerGeneratedAttribute), false) && !t.Name.Contains('<')
                )
                .OrderBy(t => t.FullName),
        ];

    public static IReadOnlyList<Type> InternalByNaming(Assembly assembly) =>
        [
            .. ExportedTypes(assembly)
                .Where(t =>
                    (t.Namespace?.Split('.').Intersect(_internalNamespaceSegments).Any() ?? false)
                    || _internalNameSuffixes.Any(s => t.Name.EndsWith(s))
                ),
        ];

    public static IReadOnlyList<Type> Unreachable(
        Assembly assembly,
        Func<Type, bool> isRoot,
        IEnumerable<Type> allowed
    )
    {
        var exported = ExportedTypes(assembly);
        var reached = new HashSet<Type>();
        var pending = new Queue<Type>(exported.Where(isRoot).Concat(allowed));
        while (pending.TryDequeue(out var type))
        {
            if (!reached.Add(type))
                continue;
            foreach (var next in SignatureTypes(type).Where(t => t.Assembly == assembly))
                pending.Enqueue(next);
        }
        return [.. exported.Where(t => !reached.Contains(t))];
    }

    private static IEnumerable<Type> SignatureTypes(Type type)
    {
        var direct = new List<Type?> { type.BaseType, type.DeclaringType };
        direct.AddRange(type.GetInterfaces());
        direct.AddRange(type.GetNestedTypes(BindingFlags.Public));
        direct.AddRange(type.GetFields(PUBLIC_MEMBERS).Select(f => f.FieldType));
        direct.AddRange(type.GetProperties(PUBLIC_MEMBERS).Select(p => p.PropertyType));
        direct.AddRange(type.GetEvents(PUBLIC_MEMBERS).Select(e => e.EventHandlerType));
        var methods = type.GetMethods(PUBLIC_MEMBERS)
            .Cast<MethodBase>()
            .Concat(type.GetConstructors(PUBLIC_MEMBERS));
        foreach (var method in methods)
        {
            if (method is MethodInfo info)
                direct.Add(info.ReturnType);
            direct.AddRange(method.GetParameters().Select(p => p.ParameterType));
        }
        return direct.OfType<Type>().SelectMany(Unwrap);
    }

    private static IEnumerable<Type> Unwrap(Type type)
    {
        if (type.HasElementType)
            return Unwrap(type.GetElementType()!);
        if (type.IsGenericType)
            return
            [
                type.GetGenericTypeDefinition(),
                .. type.GetGenericArguments().SelectMany(Unwrap),
            ];
        return [type];
    }
}
