namespace Corely.IAM.DevTools.Commands.Provider;

internal partial class Provider
{
    internal class List : CommandBase
    {
        public List()
            : base("list", "List available database providers") { }

        protected override void Execute()
        {
            Info("Available database providers:");
            foreach (var provider in DatabaseProviderExtensions.GetNames())
            {
                Info($"  - {provider}");
            }
        }
    }
}
