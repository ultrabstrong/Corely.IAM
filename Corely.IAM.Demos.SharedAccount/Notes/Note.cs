namespace Corely.IAM.Demos.SharedAccount.Notes;

public class Note
{
    public Guid Id { get; set; }

    public Guid AccountId { get; set; }

    public string AuthorUsername { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
}
