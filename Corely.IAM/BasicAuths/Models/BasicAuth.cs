using Corely.Security.Hashing.Models;

namespace Corely.IAM.BasicAuths.Models;

internal class BasicAuth
{
    public const int PASSWORD_MAX_LENGTH = 250;

    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public IHashedValue Password { get; set; } = null!;
    public DateTime? ModifiedUtc { get; set; }
}
