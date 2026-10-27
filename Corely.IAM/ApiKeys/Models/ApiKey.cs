using Corely.IAM.Models;

namespace Corely.IAM.ApiKeys.Models;

public class ApiKey
{
    public const string RESOURCE_TYPE = "api_key";
    public const int NAME_MIN_LENGTH = 1;
    public const int NAME_MAX_LENGTH = 100;
    public const int DESCRIPTION_MAX_LENGTH = 500;

    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime? LockedUtc { get; set; }
    public int TotalSuccessfulLogins { get; set; }
    public DateTime? LastSuccessfulLoginUtc { get; set; }
    public int FailedLoginsSinceLastSuccess { get; set; }
    public int TotalFailedLogins { get; set; }
    public DateTime? LastFailedLoginUtc { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }
    public List<ApiKeySecretInfo> Secrets { get; set; } = [];
    public List<ChildRef>? Groups { get; set; }
    public List<ChildRef>? Roles { get; set; }
}
