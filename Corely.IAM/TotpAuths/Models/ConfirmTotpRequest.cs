namespace Corely.IAM.TotpAuths.Models;

public record ConfirmTotpRequest(string Code)
{
    public const int CODE_LENGTH = 6;
}
