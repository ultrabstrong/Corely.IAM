using Corely.IAM.Demos.SharedAccount.Notes;
using Corely.IAM.Models;
using Corely.IAM.Services;
using Microsoft.EntityFrameworkCore;

namespace Corely.IAM.Demos.SharedAccount;

/// <summary>
/// Seeds one team with an owner and two members, plus a user with no team so the create-or-join
/// screen can be seen. Uses the same services the pages call. Skipped entirely once the owner exists.
/// </summary>
internal static class DemoSeed
{
    public const string PASSWORD = "Test1234";
    private const string DEVICE_ID = "demo-seed";
    private const string TEAM_NAME = "Acme";

    private static readonly (string Username, string Email)[] Members =
    [
        ("bobby", "bobby@example.com"),
        ("carla", "carla@example.com"),
    ];

    private static readonly (string Author, string Text)[] Notes =
    [
        ("olivia", "Kickoff is Monday at 10"),
        ("bobby", "Draft agenda is in the shared drive"),
        ("carla", "I'll book the room"),
    ];

    public static async Task RunAsync(IServiceProvider services)
    {
        var owner = await RegisterAsync(services, "olivia", "olivia@example.com");
        if (owner == null)
        {
            Console.WriteLine("Already seeded - olivia exists. Drop the databases to reseed.");
            return;
        }

        var memberIds = new List<Guid>();
        foreach (var (username, email) in Members)
            memberIds.Add((await RegisterAsync(services, username, email))!.Value);
        await RegisterAsync(services, "dana.solo", "dana.solo@example.com");

        // Creating an account needs a signed-in user; that user becomes its owner.
        Guid accountId;
        using (var scope = services.CreateScope())
        {
            await SignInAsync(scope.ServiceProvider, "olivia", accountId: null);
            var account = await scope
                .ServiceProvider.GetRequiredService<IRegistrationService>()
                .RegisterAccountAsync(new RegisterAccountRequest(TEAM_NAME, owner.Value));
            accountId = account.CreatedAccountId;
        }

        // Adding members directly needs the owner signed in to that account. The app itself uses
        // invitations; a seed has no one to hand a token to.
        using (var scope = services.CreateScope())
        {
            await SignInAsync(scope.ServiceProvider, "olivia", accountId);
            var registration = scope.ServiceProvider.GetRequiredService<IRegistrationService>();
            foreach (var memberId in memberIds)
            {
                var added = await registration.RegisterUserWithAccountAsync(
                    new RegisterUserWithAccountRequest(memberId, accountId)
                );
                if (added.ResultCode != RegisterUserWithAccountResultCode.Success)
                    throw new InvalidOperationException($"Adding member failed: {added.Message}");
            }
        }

        var timeProvider = services.GetRequiredService<TimeProvider>();
        await using var db = await services
            .GetRequiredService<IDbContextFactory<NotesDbContext>>()
            .CreateDbContextAsync();
        db.Notes.AddRange(
            Notes.Select(n => new Note
            {
                Id = Guid.CreateVersion7(),
                AccountId = accountId,
                AuthorUsername = n.Author,
                Text = n.Text,
                CreatedUtc = timeProvider.GetUtcNow().UtcDateTime,
            })
        );
        await db.SaveChangesAsync();

        Console.WriteLine($"Seeded team {TEAM_NAME}: owner olivia, members bobby and carla");
        Console.WriteLine("Seeded dana.solo with no team");
        Console.WriteLine($"Password for every seeded user: {PASSWORD}");
    }

    private static async Task<Guid?> RegisterAsync(
        IServiceProvider services,
        string username,
        string email
    )
    {
        using var scope = services.CreateScope();
        var result = await scope
            .ServiceProvider.GetRequiredService<IRegistrationService>()
            .RegisterUserAsync(new RegisterUserRequest(username, email, PASSWORD));
        return result.ResultCode == RegisterUserResultCode.Success ? result.CreatedUserId : null;
    }

    private static async Task SignInAsync(IServiceProvider scoped, string username, Guid? accountId)
    {
        var result = await scoped
            .GetRequiredService<IAuthenticationService>()
            .SignInAsync(new SignInRequest(username, PASSWORD, DEVICE_ID, accountId));
        if (result.ResultCode != SignInResultCode.Success)
            throw new InvalidOperationException(
                $"Seed sign-in failed for {username}: {result.Message}"
            );
    }
}
