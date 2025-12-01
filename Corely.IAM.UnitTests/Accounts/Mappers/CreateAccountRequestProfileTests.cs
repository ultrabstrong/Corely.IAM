using Corely.IAM.Accounts.Models;
using Corely.IAM.UnitTests.Mappers.AutoMapper;

namespace Corely.IAM.UnitTests.Accounts.Mappers;

public class CreateAccountRequestProfileTests : ProfileDelegateTestsBase
{
    private class Delegate : ProfileTestsBase<CreateAccountRequest, Account>;

    protected override ProfileTestsBase GetDelegate() => new Delegate();
}
