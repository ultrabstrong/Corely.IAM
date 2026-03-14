namespace Corely.IAM.GoogleAuths.Models;

public enum RegisterUserWithGoogleResultCode
{
    Success,
    InvalidGoogleTokenError,
    GoogleAccountInUseError,
    UserExistsError,
    ValidationError,
}

public record RegisterUserWithGoogleResult(
    RegisterUserWithGoogleResultCode ResultCode,
    string Message,
    Guid CreatedUserId
);
