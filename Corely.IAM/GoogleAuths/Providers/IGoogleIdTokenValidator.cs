using Corely.IAM.GoogleAuths.Models;

namespace Corely.IAM.GoogleAuths.Providers;

internal interface IGoogleIdTokenValidator
{
    Task<GoogleIdTokenPayload?> ValidateAsync(string idToken);
}
