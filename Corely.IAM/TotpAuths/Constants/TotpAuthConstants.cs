namespace Corely.IAM.TotpAuths.Constants;

public static class TotpAuthConstants
{
    public const int ENCRYPTED_SECRET_MAX_LENGTH = 500;
    public const int RECOVERY_CODE_LENGTH = 8;
    public const int RECOVERY_CODE_DISPLAY_LENGTH = 9; // "XXXX-XXXX"
    public const int RECOVERY_CODE_HASH_MAX_LENGTH = 250;
    public const int DEFAULT_RECOVERY_CODE_COUNT = 10;
    public const int TOTP_CODE_LENGTH = 6;
}
