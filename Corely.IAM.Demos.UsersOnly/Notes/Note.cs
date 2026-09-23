namespace Corely.IAM.Demos.UsersOnly.Notes;

public class Note
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Text { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
}
