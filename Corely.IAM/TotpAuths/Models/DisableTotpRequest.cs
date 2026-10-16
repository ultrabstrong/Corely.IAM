namespace Corely.IAM.TotpAuths.Models;

public record DisableTotpRequest(string Code)
{
    public const int CODE_LENGTH = ConfirmTotpRequest.CODE_LENGTH;
}
