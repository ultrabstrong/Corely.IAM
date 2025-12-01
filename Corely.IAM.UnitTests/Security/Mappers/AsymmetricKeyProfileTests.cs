using AutoFixture;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Security.Entities;
using Corely.IAM.Security.Models;
using Corely.IAM.UnitTests.Mappers.AutoMapper;
using Corely.IAM.Users.Entities;
using Corely.Security.Encryption;

namespace Corely.IAM.UnitTests.Security.Mappers;

public class AsymmetricKeyProfileTests
{
    public class ToAsymmetricKeyEntity : BidirectionalProfileDelegateTestsBase
    {
        private class Delegate : BidirectionalProfileTestsBase<AsymmetricKey, AsymmetricKeyEntity>
        {
            protected override AsymmetricKeyEntity ApplyDestinatonModifications(
                AsymmetricKeyEntity destination
            )
            {
                destination.EncryptedPrivateKey =
                    $"{AsymmetricEncryptionConstants.RSA_CODE}:{new Fixture().Create<string>()}";
                return destination;
            }
        }

        protected override BidirectionalProfileTestsBase GetDelegate() => new Delegate();
    }

    public class ToAccountAsymmetricKeyEntity : BidirectionalProfileDelegateTestsBase
    {
        private class Delegate
            : BidirectionalProfileTestsBase<AsymmetricKey, AccountAsymmetricKeyEntity>
        {
            protected override AccountAsymmetricKeyEntity ApplyDestinatonModifications(
                AccountAsymmetricKeyEntity destination
            )
            {
                destination.EncryptedPrivateKey =
                    $"{AsymmetricEncryptionConstants.RSA_CODE}:{new Fixture().Create<string>()}";
                return destination;
            }
        }

        protected override BidirectionalProfileTestsBase GetDelegate() => new Delegate();
    }

    public class ToUserAsymmetricKeyEntity : BidirectionalProfileDelegateTestsBase
    {
        private class Delegate
            : BidirectionalProfileTestsBase<AsymmetricKey, UserAsymmetricKeyEntity>
        {
            protected override UserAsymmetricKeyEntity ApplyDestinatonModifications(
                UserAsymmetricKeyEntity destination
            )
            {
                destination.EncryptedPrivateKey =
                    $"{AsymmetricEncryptionConstants.RSA_CODE}:{new Fixture().Create<string>()}";
                return destination;
            }
        }

        protected override BidirectionalProfileTestsBase GetDelegate() => new Delegate();
    }
}
