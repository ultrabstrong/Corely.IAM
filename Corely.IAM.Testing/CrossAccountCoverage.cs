namespace Corely.IAM.Testing;

public sealed record CrossAccountCoverage(
    IReadOnlyList<string> Uncovered,
    IReadOnlyList<string> Stale,
    IReadOnlyList<string> Duplicated,
    IReadOnlyList<string> Unreasoned
)
{
    public bool IsComplete =>
        Uncovered.Count + Stale.Count + Duplicated.Count + Unreasoned.Count == 0;

    public static IReadOnlyList<string> MethodsOf(IEnumerable<Type> services) =>
        [.. services.SelectMany(t => t.GetMethods().Select(m => $"{t.Name}.{m.Name}")).Distinct()];

    public static CrossAccountCoverage Check(
        IEnumerable<Type> services,
        IEnumerable<string> attacked,
        IReadOnlyDictionary<string, string> exemptions
    )
    {
        var methods = MethodsOf(services).ToHashSet(StringComparer.Ordinal);
        var attacks = attacked.ToHashSet(StringComparer.Ordinal);
        var exempt = exemptions.Keys.ToHashSet(StringComparer.Ordinal);

        return new(
            [.. methods.Except(attacks).Except(exempt).Order(StringComparer.Ordinal)],
            [.. attacks.Union(exempt).Except(methods).Order(StringComparer.Ordinal)],
            [.. attacks.Intersect(exempt).Order(StringComparer.Ordinal)],
            [
                .. exemptions
                    .Where(e => string.IsNullOrWhiteSpace(e.Value))
                    .Select(e => e.Key)
                    .Order(StringComparer.Ordinal),
            ]
        );
    }

    public static async Task<IReadOnlyList<string>> FindReachedAsync(
        IReadOnlyDictionary<string, Func<Task<bool>>> attacks
    )
    {
        var reached = new List<string>();
        foreach (var (method, wasRefused) in attacks)
        {
            if (!await wasRefused())
                reached.Add(method);
        }
        return reached;
    }

    public override string ToString() =>
        string.Join(
            " ",
            new[]
            {
                Describe("Add an attack or a reasoned exemption for", Uncovered),
                Describe("Remove what no longer exists", Stale),
                Describe("Keep either the attack or the exemption for", Duplicated),
                Describe("Give a reason for the exemption of", Unreasoned),
            }.Where(line => line.Length > 0)
        );

    private static string Describe(string instruction, IReadOnlyList<string> methods) =>
        methods.Count == 0 ? string.Empty : $"{instruction}: {string.Join(", ", methods)}.";
}
