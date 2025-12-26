namespace Corely.IAM.Models;

public record SwitchAccountRequest(string AuthToken, Guid AccountPublicId);
