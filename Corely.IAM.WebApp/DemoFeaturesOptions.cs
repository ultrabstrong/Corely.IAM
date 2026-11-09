namespace Corely.IAM.WebApp;

public sealed class DemoFeaturesOptions
{
    public const string NAME = "DemoFeatures";

    public bool EnablePasswordRecoveryPreview { get; set; } = true;

    /// <summary>
    /// How the reference app delivers invitations, to show each outcome: <c>Off</c> registers no
    /// sender, so the inviter gets the link; <c>Send</c> puts the invitation on the preview page;
    /// <c>Fail</c> registers a sender that always fails, so the inviter gets the link again.
    /// </summary>
    public InvitationPreviewMode InvitationPreview { get; set; } = InvitationPreviewMode.Send;

    public string PlatformOwnerTotpSecret { get; set; } = string.Empty;
}
