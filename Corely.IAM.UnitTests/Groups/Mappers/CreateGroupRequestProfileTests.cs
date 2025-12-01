using Corely.IAM.Groups.Models;
using Corely.IAM.UnitTests.Mappers.AutoMapper;

namespace Corely.IAM.UnitTests.Groups.Mappers;

public class CreateGroupRequestProfileTests : ProfileDelegateTestsBase
{
    private class Delegate : ProfileTestsBase<CreateGroupRequest, Group>;

    protected override ProfileTestsBase GetDelegate() => new Delegate();
}
