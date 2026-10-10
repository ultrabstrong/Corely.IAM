using Corely.IAM.Models;
using Corely.IAM.Platform.Models;

namespace Corely.IAM.Audits.Models;

public record GetPlatformSettingsResult(
    RetrieveResultCode ResultCode,
    string Message,
    PlatformSettings? Settings
);
