using Corely.Security.Hashing.Models;

namespace Corely.IAM.BasicAuths.Models;

public class BasicAuth
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public IHashedValue Password { get; set; } = null!;
    public DateTime? ModifiedUtc { get; set; }
}
