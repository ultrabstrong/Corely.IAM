using Corely.IAM.IdentityProviders.Enums;
using Corely.IAM.IdentityProviders.Types;

namespace Corely.IAM.UnitTests.IdentityProviders.Types;

public class IdentityProviderDefinitionTests
{
    private readonly IdentityProviderCatalog _catalog = new();

    [Theory]
    [InlineData(IdentityProviderType.Google)]
    [InlineData(IdentityProviderType.Microsoft)]
    [InlineData(IdentityProviderType.GitHub)]
    public void Link_ShowsTheAccountPicker(IdentityProviderType type)
    {
        var parameters = _catalog.Get(type).AuthorizationParameters(FederatedSignInPurpose.Link);

        Assert.Equal(
            IdentityProviderDefinition.SELECT_ACCOUNT_PROMPT,
            parameters[IdentityProviderDefinition.PROMPT_PARAMETER]
        );
    }

    [Theory]
    [InlineData(IdentityProviderType.Facebook)]
    [InlineData(IdentityProviderType.Amazon)]
    public void Link_AddsNoPrompt_WhereTheProviderHasNoAccountPicker(IdentityProviderType type)
    {
        Assert.Empty(_catalog.Get(type).AuthorizationParameters(FederatedSignInPurpose.Link));
    }

    [Theory]
    [InlineData(IdentityProviderType.Google)]
    [InlineData(IdentityProviderType.Microsoft)]
    [InlineData(IdentityProviderType.GitHub)]
    [InlineData(IdentityProviderType.Facebook)]
    [InlineData(IdentityProviderType.Amazon)]
    public void SignIn_AddsNoPrompt(IdentityProviderType type)
    {
        Assert.Empty(_catalog.Get(type).AuthorizationParameters(FederatedSignInPurpose.SignIn));
    }

    [Fact]
    public void Microsoft_Reauthentication_ForcesAFreshSignIn_NotTheAccountPicker()
    {
        var parameters = _catalog
            .Get(IdentityProviderType.Microsoft)
            .AuthorizationParameters(FederatedSignInPurpose.Reauthenticate);

        Assert.Equal("login", parameters[IdentityProviderDefinition.PROMPT_PARAMETER]);
        Assert.Equal("0", parameters["max_age"]);
    }
}
