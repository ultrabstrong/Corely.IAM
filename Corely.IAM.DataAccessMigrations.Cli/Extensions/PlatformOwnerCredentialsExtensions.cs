using System.Text;
using Corely.IAM.Platform.Models;

namespace Corely.IAM.DataAccessMigrations.Cli.Extensions;

internal static class PlatformOwnerCredentialsExtensions
{
    extension(PlatformOwnerCredentials credentials)
    {
        public string ToSecretsFileText(DateTimeOffset createdUtc)
        {
            var text = new StringBuilder()
                .AppendLine("Corely IAM platform owner")
                .AppendLine($"Created (UTC): {createdUtc:yyyy-MM-dd HH:mm:ss}")
                .AppendLine()
                .AppendLine($"Account: {credentials.AccountName} ({credentials.AccountId})")
                .AppendLine($"Username: {credentials.Username}")
                .AppendLine($"Email: {credentials.Email}")
                .AppendLine($"Password: {credentials.Password}")
                .AppendLine()
                .AppendLine($"Authenticator secret: {credentials.TotpSecret}")
                .AppendLine($"Authenticator setup URI: {credentials.TotpSetupUri}")
                .AppendLine()
                .AppendLine("Recovery codes (each works once):");
            foreach (var code in credentials.RecoveryCodes)
            {
                text.AppendLine($"  {code}");
            }
            return text.ToString();
        }
    }
}
