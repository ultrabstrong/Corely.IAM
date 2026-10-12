namespace Corely.IAM.Platform.Models;

public enum BootstrapPlatformResultCode
{
    Success,
    AlreadyBootstrappedError,
    OwnerCreationError,
    OwnerSignInError,
    AccountCreationError,
    TwoFactorEnrollmentError,
    InvalidSecuritySettingsError,
}

public record BootstrapPlatformResult(
    BootstrapPlatformResultCode ResultCode,
    string Message,
    PlatformOwnerCredentials? Credentials
);
