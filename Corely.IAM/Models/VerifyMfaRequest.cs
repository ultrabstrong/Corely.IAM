namespace Corely.IAM.Models;

public record VerifyMfaRequest(string MfaChallengeToken, string Code);
