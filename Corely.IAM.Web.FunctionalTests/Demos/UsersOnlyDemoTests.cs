extern alias UsersOnlyDemo;

using UsersOnlyDemo::Corely.IAM.Demos.UsersOnly.Notes;

namespace Corely.IAM.Web.FunctionalTests.Demos;

public class UsersOnlyDemoTests : DemoAppTestsBase<NotesDbContext>
{
    protected override string AppName => "My Notes";
}
