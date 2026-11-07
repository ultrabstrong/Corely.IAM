using Corely.IAM.IdentityProviders.Enums;

namespace Corely.IAM.IdentityProviders.Types;

internal sealed class IdentityProviderCatalog
{
    private readonly IReadOnlyDictionary<
        IdentityProviderType,
        IdentityProviderDefinition
    > _definitions;

    public IdentityProviderCatalog()
        : this(
            new GoogleProviderDefinition(),
            new MicrosoftProviderDefinition(),
            new GitHubProviderDefinition(),
            new FacebookProviderDefinition(),
            new AmazonProviderDefinition()
        ) { }

    public IdentityProviderCatalog(params IdentityProviderDefinition[] definitions) =>
        _definitions = definitions.ToDictionary(d => d.Type);

    public IReadOnlyCollection<IdentityProviderDefinition> All => [.. _definitions.Values];

    public IdentityProviderDefinition Get(IdentityProviderType type) => _definitions[type];
}
