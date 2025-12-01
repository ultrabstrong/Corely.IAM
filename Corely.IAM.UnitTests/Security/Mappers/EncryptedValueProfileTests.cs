using AutoFixture;
using Corely.IAM.UnitTests.Mappers.AutoMapper;
using Corely.Security.Encryption;
using Corely.Security.Encryption.Models;
using Corely.Security.Encryption.Providers;

namespace Corely.IAM.UnitTests.Security.Mappers;

public class EncryptedValueProfileTests
    : BidirectionalProfileTestsBase<SymmetricEncryptedValue, string>
{
    protected override string GetDestination() =>
        $"{SymmetricEncryptionConstants.AES_CODE}:{new Fixture().Create<string>()}";

    protected override object[] GetSourceParams() => [Mock.Of<ISymmetricEncryptionProvider>()];
}
