using Corely.IAM.UnitTests.Mappers.AutoMapper;
using Corely.IAM.Users.Models;

namespace Corely.IAM.UnitTests.Users.Mappers;

public class CreateUserRequestTests : ProfileDelegateTestsBase
{
    private class Delegate : ProfileTestsBase<CreateUserRequest, User>;

    protected override ProfileTestsBase GetDelegate() => new Delegate();
}
