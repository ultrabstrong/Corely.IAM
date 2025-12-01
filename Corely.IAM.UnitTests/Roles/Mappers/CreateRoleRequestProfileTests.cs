using Corely.IAM.Roles.Models;
using Corely.IAM.UnitTests.Mappers.AutoMapper;

namespace Corely.IAM.UnitTests.Roles.Mappers;

public class CreateRoleRequestProfileTests : ProfileDelegateTestsBase
{
    private class Delegate : ProfileTestsBase<CreateRoleRequest, Role>;

    protected override ProfileTestsBase GetDelegate() => new Delegate();
}
