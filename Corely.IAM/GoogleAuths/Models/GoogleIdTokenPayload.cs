namespace Corely.IAM.GoogleAuths.Models;

internal record GoogleIdTokenPayload(string Subject, string Email, bool EmailVerified);
