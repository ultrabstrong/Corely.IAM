using AutoFixture;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Security.Entities;
using Corely.IAM.Security.Models;
using Corely.IAM.UnitTests.Mappers.AutoMapper;
using Corely.IAM.Users.Entities;
using Corely.Security.Encryption;

namespace Corely.IAM.UnitTests.Security.Mappers;

public class SymmetricKeyProfileTests
{
    public class ToSymmetricKeyEntity : BidirectionalProfileDelegateTestsBase
    {
        private class Delegate : BidirectionalProfileTestsBase<SymmetricKey, SymmetricKeyEntity>
        {
            protected override SymmetricKeyEntity ApplyDestinatonModifications(
                SymmetricKeyEntity destination
            )
            {
                destination.EncryptedKey =
                    $"{SymmetricEncryptionConstants.AES_CODE}:{new Fixture().Create<string>()}";
                return destination;
            }
        }

        protected override BidirectionalProfileTestsBase GetDelegate() => new Delegate();
    }

    public class ToAccountSymmetricKeyEntity : BidirectionalProfileDelegateTestsBase
    {
        private class Delegate
            : BidirectionalProfileTestsBase<SymmetricKey, AccountSymmetricKeyEntity>
        {
            protected override AccountSymmetricKeyEntity ApplyDestinatonModifications(
                AccountSymmetricKeyEntity destination
            )
            {
                destination.EncryptedKey =
                    $"{SymmetricEncryptionConstants.AES_CODE}:{new Fixture().Create<string>()}";
                return destination;
            }
        }

        protected override BidirectionalProfileTestsBase GetDelegate() => new Delegate();
    }

    public class ToUserSymmetricKeyEntity : BidirectionalProfileDelegateTestsBase
    {
        private class Delegate : BidirectionalProfileTestsBase<SymmetricKey, UserSymmetricKeyEntity>
        {
            protected override UserSymmetricKeyEntity ApplyDestinatonModifications(
                UserSymmetricKeyEntity destination
            )
            {
                destination.EncryptedKey =
                    $"{SymmetricEncryptionConstants.AES_CODE}:{new Fixture().Create<string>()}";
                return destination;
            }
        }

        protected override BidirectionalProfileTestsBase GetDelegate() => new Delegate();
    }
}
