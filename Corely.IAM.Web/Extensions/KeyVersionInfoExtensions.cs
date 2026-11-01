using Corely.IAM.Security.Models;

namespace Corely.IAM.Web.Extensions;

internal static class KeyVersionInfoExtensions
{
    extension(KeyVersionInfo info)
    {
        public string OptionLabel(int currentVersion) =>
            info.Version == currentVersion ? $"Version {info.Version} (current)"
            : info.IsCompromised ? $"Version {info.Version} (compromised)"
            : $"Version {info.Version}";
    }
}
