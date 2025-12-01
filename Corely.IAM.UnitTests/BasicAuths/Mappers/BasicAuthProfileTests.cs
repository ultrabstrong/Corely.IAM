using Corely.IAM.BasicAuths.Entities;
using Corely.IAM.UnitTests.Mappers.AutoMapper;
using Corely.Security.Hashing;

namespace Corely.IAM.UnitTests.BasicAuths.Mappers;

public class BasicAuthProfileTests : BidirectionalProfileDelegateTestsBase
{
    private class Delegate
        : BidirectionalProfileTestsBase<Corely.IAM.BasicAuths.Models.BasicAuth, BasicAuthEntity>
    {
        protected override BasicAuthEntity ApplyDestinatonModifications(BasicAuthEntity destination)
        {
            destination.Password = $"{HashConstants.SALTED_SHA256_CODE}:{destination.Password}";
            return destination;
        }
    }

    protected override BidirectionalProfileTestsBase GetDelegate() => new Delegate();
}
