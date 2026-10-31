using System.Text.RegularExpressions;
using Corely.IAM.Services;

namespace Corely.IAM.Web.UnitTests.Components;

public class AssociationSidesTests
{
    private const string USER = "Users/UserDetail.razor";
    private const string GROUP = "Groups/GroupDetail.razor";
    private const string ROLE = "Roles/RoleDetail.razor";
    private const string API_KEY = "ApiKeys/ApiKeyDetail.razor";
    private const string PERMISSION = "Permissions/PermissionDetail.razor";

    private sealed record Association(
        string Add,
        string Remove,
        string FirstPage,
        string SecondPage
    );

    private static readonly Association[] _associations =
    [
        new(
            nameof(IRegistrationService.RegisterUsersWithGroupAsync),
            nameof(IDeregistrationService.DeregisterUsersFromGroupAsync),
            USER,
            GROUP
        ),
        new(
            nameof(IRegistrationService.RegisterRolesWithUserAsync),
            nameof(IDeregistrationService.DeregisterRolesFromUserAsync),
            USER,
            ROLE
        ),
        new(
            nameof(IRegistrationService.RegisterRolesWithGroupAsync),
            nameof(IDeregistrationService.DeregisterRolesFromGroupAsync),
            GROUP,
            ROLE
        ),
        new(
            nameof(IRegistrationService.RegisterRolesWithApiKeyAsync),
            nameof(IDeregistrationService.DeregisterRolesFromApiKeyAsync),
            API_KEY,
            ROLE
        ),
        new(
            nameof(IRegistrationService.RegisterApiKeysWithGroupAsync),
            nameof(IDeregistrationService.DeregisterApiKeysFromGroupAsync),
            API_KEY,
            GROUP
        ),
        new(
            nameof(IRegistrationService.RegisterPermissionsWithRoleAsync),
            nameof(IDeregistrationService.DeregisterPermissionsFromRoleAsync),
            ROLE,
            PERMISSION
        ),
    ];

    private static readonly Dictionary<string, string> _exempt = new()
    {
        [nameof(IRegistrationService.RegisterUserWithGoogleAsync)] =
            "Creates a user from a Google sign in; it links nothing.",
        [nameof(IRegistrationService.RegisterUserWithAccountAsync)] =
            "A user joins an account by invitation: the account page creates it and the user accepts it, because no account may list users outside itself.",
        [nameof(IDeregistrationService.DeregisterUserFromAccountAsync)] =
            "Removed from either side already: the account and user list pages remove a member and the profile page leaves.",
    };

    private static string ComponentsFolder()
    {
        var folder = new DirectoryInfo(AppContext.BaseDirectory);
        while (folder != null && !File.Exists(Path.Combine(folder.FullName, "Corely.IAM.slnx")))
            folder = folder.Parent;

        Assert.NotNull(folder);
        return Path.Combine(folder.FullName, "Corely.IAM.Web", "Components");
    }

    private static string ReadPage(string page) =>
        File.ReadAllText(Path.Combine(ComponentsFolder(), page));

    private static IEnumerable<string> FindMissingSides(
        IEnumerable<Association> associations,
        Func<string, string> readPage
    ) =>
        associations.SelectMany(a =>
            new[] { a.FirstPage, a.SecondPage }.SelectMany(page =>
            {
                var markup = readPage(page);
                return new[] { a.Add, a.Remove }
                    .Where(call => !markup.Contains($"{call}("))
                    .Concat(markup.Contains("<EntityPickerModal") ? [] : ["<EntityPickerModal"])
                    .Select(missing => $"{page} has no {missing}");
            })
        );

    [Fact]
    public void EveryAssociation_CanBeAddedAndRemoved_FromBothItsPages()
    {
        Assert.Empty(FindMissingSides(_associations, ReadPage));
    }

    [Fact]
    public void FindMissingSides_ReportsAPageWithoutTheCall()
    {
        var association = new Association("AddAsync", "RemoveAsync", "first", "second");

        var missing = FindMissingSides(
            [association],
            page =>
                page == "first"
                    ? "<EntityPickerModal AddAsync() RemoveAsync()"
                    : "<EntityPickerModal RemoveAsync()"
        );

        Assert.Equal(["second has no AddAsync"], missing);
    }

    [Theory]
    [InlineData(typeof(IRegistrationService), @"^Register\w+With\w+Async$")]
    [InlineData(typeof(IDeregistrationService), @"^Deregister\w+From\w+Async$")]
    public void EveryLinkingServiceMethod_IsInTheTable_OrExemptWithAReason(
        Type service,
        string pattern
    )
    {
        var listed = _associations.SelectMany(a => new[] { a.Add, a.Remove }).ToHashSet();

        var unlisted = service
            .GetMethods()
            .Select(m => m.Name)
            .Where(name => Regex.IsMatch(name, pattern))
            .Where(name => !listed.Contains(name) && !_exempt.ContainsKey(name));

        Assert.Empty(unlisted);
    }
}
