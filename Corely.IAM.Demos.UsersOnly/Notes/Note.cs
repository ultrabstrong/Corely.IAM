namespace Corely.IAM.Demos.UsersOnly.Notes;

public class Note
{
    public Guid Id { get; set; }

    // The IAM user id. A user owns their notes outright, so this is the whole authorization model.
    public Guid UserId { get; set; }

    public string Text { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
}
