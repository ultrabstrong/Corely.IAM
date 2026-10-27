namespace Corely.IAM.Web.Endpoints;

internal record ApiKeySignInBody(string ApiKey, string? DeviceId)
{
    public const string DEFAULT_DEVICE_ID = "api";
}
