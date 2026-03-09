namespace Corely.IAM.Security.Models;

public interface IIamAsymmetricSignatureProvider
{
    string Sign(string payload);
    bool Verify(string payload, string signature);
}
