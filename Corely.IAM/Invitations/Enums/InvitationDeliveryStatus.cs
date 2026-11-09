namespace Corely.IAM.Invitations.Enums;

public enum InvitationDeliveryStatus
{
    /// <summary>No sender is registered; the token is returned for the inviter to hand over.</summary>
    NotAttempted,

    /// <summary>The host's sender delivered it; no token is returned.</summary>
    Sent,

    /// <summary>The sender could not deliver it; the token is returned instead.</summary>
    Failed,
}
