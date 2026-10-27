namespace Corely.IAM.Platform.Models;

public sealed record ResourceTypeSet
{
    internal const char SEPARATOR = ',';

    public static ResourceTypeSet Empty { get; } = new([]);

    public IReadOnlyList<string> Types { get; }

    public ResourceTypeSet(IEnumerable<string> types)
    {
        Types =
        [
            .. types
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase),
        ];
    }

    public bool Contains(string resourceType) =>
        Types.Contains(resourceType, StringComparer.OrdinalIgnoreCase);

    public bool Equals(ResourceTypeSet? other) =>
        other is not null && Types.SequenceEqual(other.Types, StringComparer.OrdinalIgnoreCase);

    public override int GetHashCode() =>
        Types.Aggregate(0, (hash, t) => HashCode.Combine(hash, t.ToLowerInvariant()));

    public override string ToString() => string.Join(", ", Types);

    internal string ToStoredValue() => string.Join(SEPARATOR, Types);

    internal static ResourceTypeSet FromStoredValue(string? value) =>
        new(value?.Split(SEPARATOR) ?? []);
}
