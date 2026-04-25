using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Models;
using Corely.IAM.Services;

namespace Corely.IAM.DevTools.Commands.Retrieval;

internal partial class Retrieval : CommandBase
{
    internal class AccountAsymSign : CommandBase
    {
        [Argument("The account ID (GUID)", true)]
        private string Id { get; init; } = null!;

        [Option("-s", "--sign", Description = "Sign a payload")]
        private string ToSign { get; init; } = null!;

        [Option("-v", "--verify", Description = "Verify a payload (requires --signature)")]
        private string ToVerify { get; init; } = null!;

        [Option("--signature", Description = "Signature to verify against (used with --verify)")]
        private string Signature { get; init; } = null!;

        private readonly IRetrievalService _retrievalService;
        private readonly IAuthenticationService _authenticationService;

        public AccountAsymSign(
            IRetrievalService retrievalService,
            IAuthenticationService authenticationService
        )
            : base(
                "account-asym-sign",
                "Asymmetric signature operations using an account's key",
                "Requires authentication. Use flags to perform operations."
            )
        {
            _retrievalService = retrievalService.ThrowIfNull(nameof(retrievalService));
            _authenticationService = authenticationService.ThrowIfNull(
                nameof(authenticationService)
            );
        }

        protected override async Task ExecuteAsync()
        {
            if (!await SetUserContextFromAuthTokenFileAsync(_authenticationService))
                return;

            if (string.IsNullOrEmpty(ToSign) && string.IsNullOrEmpty(ToVerify))
            {
                ShowHelp("At least one operation flag is required");
                return;
            }

            var result = await _retrievalService.GetAccountAsymmetricSignatureProviderAsync(
                Guid.Parse(Id)
            );
            if (result.ResultCode != RetrieveResultCode.Success || result.Item == null)
            {
                Error($"Failed to get provider: {result.ResultCode} - {result.Message}");
                return;
            }

            if (!string.IsNullOrEmpty(ToSign))
            {
                var signature = result.Item.Sign(ToSign);
                Success($"Signature: {signature}");
            }

            if (!string.IsNullOrEmpty(ToVerify))
            {
                if (string.IsNullOrEmpty(Signature))
                {
                    Error("--signature is required when using --verify");
                    return;
                }

                var isValid = result.Item.Verify(ToVerify, Signature);
                if (isValid)
                {
                    Success("Signature is valid");
                }
                else
                {
                    Error("Signature is invalid");
                }
            }
        }
    }
}
