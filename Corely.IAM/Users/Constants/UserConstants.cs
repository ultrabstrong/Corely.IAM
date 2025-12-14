namespace Corely.IAM.Users.Constants;

public static class UserConstants
{
    public const int USERNAME_MIN_LENGTH = 5;
    public const int USERNAME_MAX_LENGTH = 30;
    public const int EMAIL_MAX_LENGTH = 254; // RFC 5321

    public const string JWT_AUDIENCE = "Corely.IAM";
    public const string ACCOUNT_ID_CLAIM = "account_id";
    public const string SIGNED_IN_ACCOUNT_ID_CLAIM = "signed_in_account_id";
}
