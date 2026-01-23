namespace Corely.IAM.DevTools.Commands.Provider;

internal partial class Provider
{
    internal class Show : CommandBase
    {
        public Show()
            : base("show", "Show the current database provider") { }

        protected override void Execute()
        {
            var provider = ConfigurationProvider.TryGetProvider();

            if (provider == null)
            {
                Warn("No provider configured.");
                Info("Run 'provider set <provider>' to set a provider.");
                Info(
                    $"Available providers: {string.Join(", ", DatabaseProviderExtensions.GetNames())}"
                );
            }
            else
            {
                Info($"Current provider: {provider}");
            }
        }
    }
}
