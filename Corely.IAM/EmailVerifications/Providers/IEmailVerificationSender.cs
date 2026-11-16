using Corely.IAM.EmailVerifications.Models;

namespace Corely.IAM.EmailVerifications.Providers;

public interface IEmailVerificationSender
{
    Task<EmailVerificationSendResult> SendAsync(EmailVerificationDelivery delivery);
}
