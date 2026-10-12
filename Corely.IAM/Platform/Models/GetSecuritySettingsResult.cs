using Corely.IAM.Models;

namespace Corely.IAM.Platform.Models;

public record GetSecuritySettingsResult(
    RetrieveResultCode ResultCode,
    string Message,
    SecurityPolicy? Settings
);
