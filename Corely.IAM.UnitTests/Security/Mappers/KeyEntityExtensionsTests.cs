using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Mappers;
using Corely.IAM.Users.Entities;

namespace Corely.IAM.UnitTests.Security.Mappers;

public class KeyEntityExtensionsTests
{
    private static UserAsymmetricKeyEntity Key(KeyUsedFor use, int generation) =>
        new() { KeyUsedFor = use, Generation = generation };

    [Fact]
    public void Generations_ReturnsOnlyThePurpose_InGenerationOrder()
    {
        var second = Key(KeyUsedFor.Signature, 2);
        var first = Key(KeyUsedFor.Signature, 1);
        List<UserAsymmetricKeyEntity> keys = [second, Key(KeyUsedFor.Encryption, 1), first];

        Assert.Equal([first, second], keys.Generations(KeyUsedFor.Signature));
    }

    [Fact]
    public void Generations_ReturnsEmpty_ForUnloadedKeys() =>
        Assert.Empty(((List<UserAsymmetricKeyEntity>?)null).Generations(KeyUsedFor.Signature));

    [Fact]
    public void CurrentGeneration_ReturnsHighestGeneration_ForPurpose()
    {
        var newest = Key(KeyUsedFor.Encryption, 3);
        List<UserAsymmetricKeyEntity> keys =
        [
            Key(KeyUsedFor.Encryption, 1),
            newest,
            Key(KeyUsedFor.Signature, 5),
            Key(KeyUsedFor.Encryption, 2),
        ];

        Assert.Same(newest, keys.CurrentGeneration(KeyUsedFor.Encryption));
    }

    [Fact]
    public void CurrentGeneration_ReturnsNull_ForMissingPurpose() =>
        Assert.Null(
            new List<UserAsymmetricKeyEntity> { Key(KeyUsedFor.Encryption, 1) }.CurrentGeneration(
                KeyUsedFor.Signature
            )
        );

    [Fact]
    public void NextGeneration_CountsOnlyThePurpose_ForMixedKeys()
    {
        List<UserAsymmetricKeyEntity> keys =
        [
            Key(KeyUsedFor.Encryption, 1),
            Key(KeyUsedFor.Encryption, 2),
            Key(KeyUsedFor.Signature, 1),
        ];

        Assert.Equal(3, keys.NextGeneration(KeyUsedFor.Encryption));
        Assert.Equal(2, keys.NextGeneration(KeyUsedFor.Signature));
    }

    [Fact]
    public void NextGeneration_ReturnsOne_ForNoKeys() =>
        Assert.Equal(1, new List<UserAsymmetricKeyEntity>().NextGeneration(KeyUsedFor.Signature));
}
