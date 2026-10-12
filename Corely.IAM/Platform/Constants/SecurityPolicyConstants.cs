namespace Corely.IAM.Platform.Constants;

public static class SecurityPolicyConstants
{
    public const int PASSWORD_MINIMUM_LENGTH_DEFAULT = 8;
    public const int PASSWORD_MINIMUM_LENGTH_MIN = 3;
    public const int PASSWORD_MINIMUM_LENGTH_MAX = 64;
    public const int PASSWORD_MINIMUM_LENGTH_RECOMMENDED = 8;

    public const int PASSWORD_MAXIMUM_LENGTH_DEFAULT = 128;
    public const int PASSWORD_MAXIMUM_LENGTH_MIN = 64;
    public const int PASSWORD_MAXIMUM_LENGTH_MAX = 256;

    public const bool REQUIRE_UPPERCASE_DEFAULT = true;
    public const bool REQUIRE_LOWERCASE_DEFAULT = true;
    public const bool REQUIRE_DIGIT_DEFAULT = true;
    public const bool REQUIRE_NON_ALPHANUMERIC_DEFAULT = false;

    public const int MAX_LOGIN_ATTEMPTS_DEFAULT = 5;
    public const int MAX_LOGIN_ATTEMPTS_MIN = 3;
    public const int MAX_LOGIN_ATTEMPTS_MAX = 20;

    public const int LOCKOUT_COOLDOWN_SECONDS_DEFAULT = 900;
    public const int LOCKOUT_COOLDOWN_SECONDS_MIN = 60;
    public const int LOCKOUT_COOLDOWN_SECONDS_MAX = 86400;

    public const int AUTH_TOKEN_TTL_SECONDS_DEFAULT = 3600;
    public const int AUTH_TOKEN_TTL_SECONDS_MIN = 300;
    public const int AUTH_TOKEN_TTL_SECONDS_MAX = 86400;

    public const int AUTH_SESSION_TTL_SECONDS_DEFAULT = 604800;
    public const int AUTH_SESSION_TTL_SECONDS_MIN = 3600;
    public const int AUTH_SESSION_TTL_SECONDS_MAX = 7776000;

    public const int MFA_CHALLENGE_TIMEOUT_SECONDS_DEFAULT = 300;
    public const int MFA_CHALLENGE_TIMEOUT_SECONDS_MIN = 60;
    public const int MFA_CHALLENGE_TIMEOUT_SECONDS_MAX = 900;

    public const int TOTP_RECOVERY_CODE_COUNT_DEFAULT = 10;
    public const int TOTP_RECOVERY_CODE_COUNT_MIN = 5;
    public const int TOTP_RECOVERY_CODE_COUNT_MAX = 20;
}
