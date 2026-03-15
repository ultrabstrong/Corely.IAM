using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.TotpAuths.Providers;

namespace Corely.IAM.DevTools.Commands.Totp;

internal partial class Totp : CommandBase
{
    internal class ValidateCode : CommandBase
    {
        [Argument("The TOTP secret (base32)", true)]
        private string Secret { get; init; } = null!;

        [Argument("The TOTP code to validate", true)]
        private string Code { get; init; } = null!;

        private readonly ITotpProvider _totpProvider;

        public ValidateCode(ITotpProvider totpProvider)
            : base("validate-code", "Validate a TOTP code against a secret")
        {
            _totpProvider = totpProvider.ThrowIfNull(nameof(totpProvider));
        }

        protected override void Execute()
        {
            var isValid = _totpProvider.ValidateCode(Secret, Code);

            if (isValid)
            {
                Success("TOTP code is valid.");
            }
            else
            {
                Error("TOTP code is invalid.");
            }
        }
    }
}
