namespace Corely.IAM.Platform.Models;

public enum BootstrapPlatformResultCode
{
    Success,
    AlreadyBootstrappedError,
    OwnerCreationError,
    OwnerSignInError,
    AccountCreationError,
    TwoFactorEnrollmentError,
    UnauthorizedError,
}

public record BootstrapPlatformResult(
    BootstrapPlatformResultCode ResultCode,
    string Message,
    PlatformOwnerCredentials? Credentials
);
