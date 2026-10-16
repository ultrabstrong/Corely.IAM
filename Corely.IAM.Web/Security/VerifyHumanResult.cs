namespace Corely.IAM.Web.Security;

public enum VerifyHumanResultCode
{
    Success,
    MissingToken,
    Rejected,
    Unavailable,
}

public record VerifyHumanResult(VerifyHumanResultCode ResultCode, string Message)
{
    public const string COMPLETE_THE_CHECK = "Please complete the check and try again.";
    public const string CHECK_UNAVAILABLE = "We could not run the check. Please try again.";

    public bool Succeeded => ResultCode == VerifyHumanResultCode.Success;

    public string? RefusalMessage =>
        ResultCode switch
        {
            VerifyHumanResultCode.Success => null,
            VerifyHumanResultCode.Unavailable => CHECK_UNAVAILABLE,
            _ => COMPLETE_THE_CHECK,
        };
}
