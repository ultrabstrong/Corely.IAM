namespace Corely.IAM.Invitations.Constants;

public static class InvitationConstants
{
    public const int TOKEN_LENGTH = 32;
    public const int TOKEN_MAX_LENGTH = 64;
    public const int EMAIL_MAX_LENGTH = 254;
    public const int DESCRIPTION_MAX_LENGTH = 200;
    public const int MIN_EXPIRY_SECONDS = 300;
    public const int MAX_EXPIRY_SECONDS = 2_592_000;
    public const int DEFAULT_EXPIRY_SECONDS = 604_800;
}
