namespace Corely.IAM.Models;

public record RetrieveSingleResult<T>(
    RetrieveResultCode ResultCode,
    string Message,
    T? Item,
    List<EffectivePermission>? EffectivePermissions
);
