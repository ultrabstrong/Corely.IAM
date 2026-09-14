namespace Corely.IAM.Demos.SharedAccount.Notes;

public class Note
{
    public Guid Id { get; set; }

    // The IAM account id. Every member of the team reads and edits the same notes.
    public Guid AccountId { get; set; }

    public string AuthorUsername { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
}
