using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Mappers;
using Corely.IAM.Users.Entities;

namespace Corely.IAM.UnitTests.Security.Mappers;

public class KeyEntityExtensionsTests
{
    private static UserAsymmetricKeyEntity Key(KeyUsedFor use, int version) =>
        new() { KeyUsedFor = use, Version = version };

    [Fact]
    public void Versions_ReturnsOnlyThePurpose_InVersionOrder()
    {
        var second = Key(KeyUsedFor.Signature, 2);
        var first = Key(KeyUsedFor.Signature, 1);
        List<UserAsymmetricKeyEntity> keys = [second, Key(KeyUsedFor.Encryption, 1), first];

        Assert.Equal([first, second], keys.Versions(KeyUsedFor.Signature));
    }

    [Fact]
    public void Versions_ReturnsEmpty_ForUnloadedKeys() =>
        Assert.Empty(((List<UserAsymmetricKeyEntity>?)null).Versions(KeyUsedFor.Signature));

    [Fact]
    public void CurrentVersion_ReturnsHighestVersion_ForPurpose()
    {
        var newest = Key(KeyUsedFor.Encryption, 3);
        List<UserAsymmetricKeyEntity> keys =
        [
            Key(KeyUsedFor.Encryption, 1),
            newest,
            Key(KeyUsedFor.Signature, 5),
            Key(KeyUsedFor.Encryption, 2),
        ];

        Assert.Same(newest, keys.CurrentVersion(KeyUsedFor.Encryption));
    }

    [Fact]
    public void CurrentVersion_ReturnsNull_ForMissingPurpose() =>
        Assert.Null(
            new List<UserAsymmetricKeyEntity> { Key(KeyUsedFor.Encryption, 1) }.CurrentVersion(
                KeyUsedFor.Signature
            )
        );

    [Fact]
    public void NextVersion_CountsOnlyThePurpose_ForMixedKeys()
    {
        List<UserAsymmetricKeyEntity> keys =
        [
            Key(KeyUsedFor.Encryption, 1),
            Key(KeyUsedFor.Encryption, 2),
            Key(KeyUsedFor.Signature, 1),
        ];

        Assert.Equal(3, keys.NextVersion(KeyUsedFor.Encryption));
        Assert.Equal(2, keys.NextVersion(KeyUsedFor.Signature));
    }

    [Fact]
    public void NextVersion_ReturnsOne_ForNoKeys() =>
        Assert.Equal(1, new List<UserAsymmetricKeyEntity>().NextVersion(KeyUsedFor.Signature));
}
