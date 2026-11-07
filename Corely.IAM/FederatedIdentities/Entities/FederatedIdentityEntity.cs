using Corely.DataAccess.Interfaces.Entities;
using Corely.IAM.IdentityProviders.Enums;
using Corely.IAM.Users.Entities;

namespace Corely.IAM.FederatedIdentities.Entities;

internal class FederatedIdentityEntity : IHasCreatedUtc, IHasModifiedUtc
{
    public const string TABLE_NAME = "FederatedIdentities";
    public const int ISSUER_MAX_LENGTH = 255;
    public const int SUBJECT_MAX_LENGTH = 255;
    public const int REAUTHENTICATION_MAX_AGE_SECONDS = 300;

    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public IdentityProviderType ProviderType { get; set; }
    public string Issuer { get; set; } = null!;
    public string Subject { get; set; } = null!;
    public string? Email { get; set; }
    public bool EmailVerified { get; set; }
    public DateTime? ReauthenticatedUtc { get; set; }
    public Guid? ReauthenticatedSessionId { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }
    public virtual UserEntity User { get; set; } = null!;
}
