using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.TotpAuths.Providers;

namespace Corely.IAM.DevTools.Commands.Totp;

internal partial class Totp : CommandBase
{
    internal class GenerateCode : CommandBase
    {
        [Argument("The TOTP secret (base32)", true)]
        private string Secret { get; init; } = null!;

        private readonly ITotpProvider _totpProvider;

        public GenerateCode(ITotpProvider totpProvider)
            : base("generate-code", "Generate a TOTP code from a secret")
        {
            _totpProvider = totpProvider.ThrowIfNull(nameof(totpProvider));
        }

        protected override void Execute()
        {
            var code = _totpProvider.GenerateCode(Secret);
            Success($"TOTP code: {code}");
        }
    }
}
