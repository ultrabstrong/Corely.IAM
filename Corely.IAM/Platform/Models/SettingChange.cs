using Corely.IAM.Platform.Enums;

namespace Corely.IAM.Platform.Models;

public record SettingChange(PlatformSetting Setting, object OldValue, object NewValue, bool Weakens)
{
    internal static SettingChange? Between<T>(
        PlatformSetting setting,
        T oldValue,
        T newValue,
        bool weakens
    )
        where T : notnull =>
        EqualityComparer<T>.Default.Equals(oldValue, newValue)
            ? null
            : new(setting, oldValue, newValue, weakens);
}
