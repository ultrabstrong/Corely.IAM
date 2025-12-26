namespace Corely.IAM.Models;

public record SignOutRequest(int UserId, string TokenId, string DeviceId, int? AccountId = null);
