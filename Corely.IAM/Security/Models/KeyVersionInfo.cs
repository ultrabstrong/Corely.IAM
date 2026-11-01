namespace Corely.IAM.Security.Models;

public record KeyVersionInfo(
    int Version,
    string ProviderName,
    string? PublicKey,
    bool IsCompromised
);
