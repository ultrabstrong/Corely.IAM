namespace Corely.IAM.WebApp;

public sealed class DemoFeaturesOptions
{
    public const string NAME = "DemoFeatures";

    public bool EnablePasswordRecoveryPreview { get; set; } = true;

    public InvitationPreviewMode InvitationPreview { get; set; } = InvitationPreviewMode.Send;

    public string PlatformOwnerTotpSecret { get; set; } = string.Empty;
}
