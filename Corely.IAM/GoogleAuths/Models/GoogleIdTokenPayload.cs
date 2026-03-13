namespace Corely.IAM.GoogleAuths.Models;

public record GoogleIdTokenPayload(string Subject, string Email, bool EmailVerified);
