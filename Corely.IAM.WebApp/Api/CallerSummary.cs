using Corely.IAM.Users.Models;

namespace Corely.IAM.WebApp.Api;

internal record CallerSummary(string PrincipalType, Guid PrincipalId, string Name, Guid? AccountId)
{
    public const string USER_PRINCIPAL_TYPE = "user";
    public const string API_KEY_PRINCIPAL_TYPE = "api_key";

    public static CallerSummary? From(UserContext? context) =>
        context switch
        {
            { User: { } user } => new(
                USER_PRINCIPAL_TYPE,
                user.Id,
                user.Username,
                context.CurrentAccount?.Id
            ),
            { ApiKey: { } apiKey } => new(
                API_KEY_PRINCIPAL_TYPE,
                apiKey.Id,
                apiKey.Name,
                context.CurrentAccount?.Id
            ),
            _ => null,
        };
}
