using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.DevTools.Commands.Retrieval;

internal partial class Retrieval : CommandBase
{
    internal class UserAsymSign : CommandBase
    {
        [Option("-s", "--sign", Description = "Sign a payload")]
        private string ToSign { get; init; } = null!;

        [Option("-v", "--verify", Description = "Verify a payload (requires --signature)")]
        private string ToVerify { get; init; } = null!;

        [Option("--signature", Description = "Signature to verify against (used with --verify)")]
        private string Signature { get; init; } = null!;

        private readonly IRetrievalService _retrievalService;
        private readonly IUserContextProvider _userContextProvider;

        public UserAsymSign(
            IRetrievalService retrievalService,
            IUserContextProvider userContextProvider
        )
            : base(
                "user-asym-sign",
                "Asymmetric signature operations using the current user's key",
                "Requires authentication. Use flags to perform operations."
            )
        {
            _retrievalService = retrievalService.ThrowIfNull(nameof(retrievalService));
            _userContextProvider = userContextProvider.ThrowIfNull(nameof(userContextProvider));
        }

        protected override async Task ExecuteAsync()
        {
            if (!await SetUserContextFromAuthTokenFileAsync(_userContextProvider))
                return;

            if (string.IsNullOrEmpty(ToSign) && string.IsNullOrEmpty(ToVerify))
            {
                ShowHelp("At least one operation flag is required");
                return;
            }

            var result = await _retrievalService.GetUserAsymmetricSignatureProviderAsync();
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
