using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Models;
using Corely.IAM.Services;

namespace Corely.IAM.DevTools.Commands.Retrieval;

internal partial class Retrieval : CommandBase
{
    internal class UserSymEncrypt : CommandBase
    {
        [Option("-e", "--encrypt", Description = "Encrypt a value")]
        private string ToEncrypt { get; init; } = null!;

        [Option("-d", "--decrypt", Description = "Decrypt a value")]
        private string ToDecrypt { get; init; } = null!;

        [Option("-r", "--reencrypt", Description = "Re-encrypt a value")]
        private string ToReEncrypt { get; init; } = null!;

        private readonly IRetrievalService _retrievalService;
        private readonly IAuthenticationService _authenticationService;

        public UserSymEncrypt(
            IRetrievalService retrievalService,
            IAuthenticationService authenticationService
        )
            : base(
                "user-sym-encrypt",
                "Symmetric encryption operations using the current user's key",
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

            if (
                string.IsNullOrEmpty(ToEncrypt)
                && string.IsNullOrEmpty(ToDecrypt)
                && string.IsNullOrEmpty(ToReEncrypt)
            )
            {
                ShowHelp("At least one operation flag is required");
                return;
            }

            var result = await _retrievalService.GetUserSymmetricEncryptionProviderAsync();
            if (result.ResultCode != RetrieveResultCode.Success || result.Item == null)
            {
                Error($"Failed to get provider: {result.ResultCode} - {result.Message}");
                return;
            }

            if (!string.IsNullOrEmpty(ToEncrypt))
            {
                var encrypted = result.Item.Encrypt(ToEncrypt);
                Success($"Encrypted: {encrypted}");
            }

            if (!string.IsNullOrEmpty(ToDecrypt))
            {
                var decrypted = result.Item.Decrypt(ToDecrypt);
                Success($"Decrypted: {decrypted}");
            }

            if (!string.IsNullOrEmpty(ToReEncrypt))
            {
                var reEncrypted = result.Item.ReEncrypt(ToReEncrypt);
                Success($"Re-encrypted: {reEncrypted}");
            }
        }
    }
}
