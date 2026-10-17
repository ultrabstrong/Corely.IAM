namespace Corely.IAM.Platform.Models;

public record SettingChange(string Name, string OldValue, string NewValue, bool Weakens)
{
    public string Describe() =>
        $"{Name}: {OldValue} to {NewValue}{(Weakens ? " (weakens security)" : "")}";

    internal static SettingChange? Between<T>(string name, T oldValue, T newValue, bool weakens) =>
        EqualityComparer<T>.Default.Equals(oldValue, newValue)
            ? null
            : new(name, Text(oldValue), Text(newValue), weakens);

    private static string Text<T>(T value) =>
        value is bool flag ? (flag ? "On" : "Off") : value?.ToString() ?? "";
}
