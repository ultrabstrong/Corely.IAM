namespace Corely.IAM.Models;

public record SwitchAccountRequest(string AuthToken, string DeviceId, Guid AccountPublicId);
