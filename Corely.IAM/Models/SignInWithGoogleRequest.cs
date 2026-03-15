namespace Corely.IAM.Models;

public record SignInWithGoogleRequest(
    string GoogleIdToken,
    string DeviceId,
    Guid? AccountId = null
);
