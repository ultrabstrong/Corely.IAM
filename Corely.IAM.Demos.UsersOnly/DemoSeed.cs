using Corely.IAM.Demos.UsersOnly.Notes;
using Corely.IAM.Models;
using Corely.IAM.Services;
using Microsoft.EntityFrameworkCore;

namespace Corely.IAM.Demos.UsersOnly;

/// <summary>
/// Registers demo users through the same service the sign-up page calls, then gives each a few
/// notes. Safe to rerun: a user who already exists is skipped along with their notes.
/// </summary>
internal static class DemoSeed
{
    public const string PASSWORD = "Demo-Pass1!";

    private static readonly (string Username, string Email, string[] Notes)[] Users =
    [
        (
            "alice",
            "alice@example.com",
            ["Buy milk", "Book the dentist", "Call the plumber about the leak"]
        ),
        ("marcus", "marcus@example.com", ["Draft the quarterly report", "Renew passport"]),
    ];

    public static async Task RunAsync(IServiceProvider services)
    {
        var timeProvider = services.GetRequiredService<TimeProvider>();
        var dbFactory = services.GetRequiredService<IDbContextFactory<NotesDbContext>>();

        foreach (var (username, email, notes) in Users)
        {
            using var scope = services.CreateScope();
            var result = await scope
                .ServiceProvider.GetRequiredService<IRegistrationService>()
                .RegisterUserAsync(new RegisterUserRequest(username, email, PASSWORD));

            if (result.ResultCode != RegisterUserResultCode.Success)
            {
                Console.WriteLine($"Skipped {username}: {result.Message}");
                continue;
            }

            await using var db = await dbFactory.CreateDbContextAsync();
            db.Notes.AddRange(
                notes.Select(text => new Note
                {
                    Id = Guid.CreateVersion7(),
                    UserId = result.CreatedUserId,
                    Text = text,
                    CreatedUtc = timeProvider.GetUtcNow().UtcDateTime,
                })
            );
            await db.SaveChangesAsync();
            Console.WriteLine($"Seeded {username} with {notes.Length} notes");
        }

        Console.WriteLine($"Password for every seeded user: {PASSWORD}");
    }
}
