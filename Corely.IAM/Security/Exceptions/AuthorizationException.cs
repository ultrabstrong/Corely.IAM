namespace Corely.IAM.Security.Exceptions;

public class AuthorizationException : Exception
{
    public string ResourceType { get; }
    public string RequiredAction { get; }
    public int? ResourceId { get; }

    public AuthorizationException(
        string resourceType,
        string requiredAction,
        int? resourceId = null
    )
        : base(BuildMessage(resourceType, requiredAction, resourceId))
    {
        ResourceType = resourceType;
        RequiredAction = requiredAction;
        ResourceId = resourceId;
    }

    public AuthorizationException(
        string resourceType,
        string requiredAction,
        int? resourceId,
        Exception innerException
    )
        : base(BuildMessage(resourceType, requiredAction, resourceId), innerException)
    {
        ResourceType = resourceType;
        RequiredAction = requiredAction;
        ResourceId = resourceId;
    }

    private static string BuildMessage(string resourceType, string requiredAction, int? resourceId)
    {
        var resource = resourceId.HasValue ? $"{resourceType} {resourceId}" : $"{resourceType}";
        return $"Authorization denied: {requiredAction} permission required for {resource}";
    }
}
