using Corely.Common.Extensions;
using Corely.IAM.Permissions.Providers;

namespace Corely.IAM.DevTools.Commands.Retrieval;

internal partial class Retrieval : CommandBase
{
    internal class ListResourceTypes(IResourceTypeRegistry resourceTypeRegistry)
        : CommandBase("list-resource-types", "List all registered resource types")
    {
        private readonly IResourceTypeRegistry _resourceTypeRegistry =
            resourceTypeRegistry.ThrowIfNull(nameof(resourceTypeRegistry));

        protected override Task ExecuteAsync()
        {
            var resourceTypes = _resourceTypeRegistry.GetAll();

            if (resourceTypes.Count == 0)
            {
                Warn("No resource types registered.");
                return Task.CompletedTask;
            }

            var nameWidth = Math.Max("Name".Length, resourceTypes.Max(rt => rt.Name.Length));

            var header = $"{"Name".PadRight(nameWidth)}  Description";
            var separator = new string('-', header.Length);

            Info(header);
            Info(separator);

            foreach (var rt in resourceTypes.OrderBy(rt => rt.Name))
            {
                Info($"{rt.Name.PadRight(nameWidth)}  {rt.Description}");
            }

            Success($"{Environment.NewLine}{resourceTypes.Count} resource type(s) found.");
            return Task.CompletedTask;
        }
    }
}
