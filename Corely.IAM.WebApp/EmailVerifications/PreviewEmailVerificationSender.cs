using Corely.IAM.EmailVerifications.Models;
using Corely.IAM.EmailVerifications.Providers;

namespace Corely.IAM.WebApp.EmailVerifications;

internal sealed class PreviewEmailVerificationSender(EmailVerificationPreviewStore store)
    : IEmailVerificationSender
{
    public Task<EmailVerificationSendResult> SendAsync(EmailVerificationDelivery delivery)
    {
        store.Last = delivery;
        return Task.FromResult(
            new EmailVerificationSendResult(EmailVerificationSendResultCode.Sent)
        );
    }
}
