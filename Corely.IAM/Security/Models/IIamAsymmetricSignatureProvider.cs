namespace Corely.IAM.Security.Models;

public interface IIamAsymmetricSignatureProvider
{
    string ProviderName { get; }
    string ProviderDescription { get; }
    string PublicKey { get; }
    string Sign(string payload);
    bool Verify(string payload, string signature);
}
