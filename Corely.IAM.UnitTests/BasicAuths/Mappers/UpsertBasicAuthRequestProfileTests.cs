using Corely.IAM.BasicAuths.Models;
using Corely.IAM.UnitTests.Mappers.AutoMapper;

namespace Corely.IAM.UnitTests.BasicAuths.Mappers;

public class UpsertBasicAuthRequestProfileTests : ProfileDelegateTestsBase
{
    private class Delegate : ProfileTestsBase<UpsertBasicAuthRequest, BasicAuth>;

    protected override ProfileTestsBase GetDelegate() => new Delegate();
}
