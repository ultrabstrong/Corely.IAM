namespace Corely.IAM.TotpAuths.Providers;

public interface ITotpProvider
{
    string GenerateSecret();
    string GenerateSetupUri(string secret, string issuer, string userLabel);
    string GenerateCode(string secret);
    bool ValidateCode(string secret, string code);
}
