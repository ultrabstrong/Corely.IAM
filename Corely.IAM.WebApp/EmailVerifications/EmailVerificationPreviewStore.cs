using Corely.IAM.EmailVerifications.Models;

namespace Corely.IAM.WebApp.EmailVerifications;

public sealed class EmailVerificationPreviewStore
{
    public EmailVerificationDelivery? Last { get; set; }
}
