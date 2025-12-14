using Corely.Security.Hashing.Models;

namespace Corely.IAM.BasicAuths.Models;

public class BasicAuth
{
    public int Id { get; init; }
    public int UserId { get; set; }
    public IHashedValue Password { get; set; } = null!;
    public DateTime? ModifiedUtc { get; init; }
}
