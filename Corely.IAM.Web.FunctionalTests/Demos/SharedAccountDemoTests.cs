// Aliased: every host's top-level Program is public and shares one global name.
extern alias SharedAccountDemo;

using SharedAccountDemo::Corely.IAM.Demos.SharedAccount.Notes;

namespace Corely.IAM.Web.FunctionalTests.Demos;

public class SharedAccountDemoTests : DemoAppTestsBase<NotesDbContext>
{
    protected override string AppName => "Team Notes";
}
