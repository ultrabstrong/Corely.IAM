using Corely.IAM.IntegrationTests.Infrastructure;
using Corely.IAM.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.IntegrationTests.Persistence;

public class KeyRotationTests(IamScenario scenario) : IClassFixture<IamScenario>
{
    [Fact]
    public async Task RotatingAUserEncryptionKey_KeepsEarlierCiphertextReadable()
    {
        var (before, after) = await scenario.ActAsAsync(
            scenario.DirectMemberUsername,
            null,
            async services =>
            {
                var retrieval = services.GetRequiredService<IRetrievalService>();
                var first = await retrieval.GetUserSymmetricEncryptionProviderAsync();
                var ciphertext = first.Item!.Encrypt("kept");

                var rotated = await services
                    .GetRequiredService<IModificationService>()
                    .RotateCurrentUserKeyAsync(KeyType.SymmetricEncryption);
                Assert.Equal(ModifyResultCode.Success, rotated.ResultCode);

                var second = await retrieval.GetUserSymmetricEncryptionProviderAsync();
                return (ciphertext, second.Item!);
            }
        );

        Assert.Equal("kept", after.Decrypt(before));
        Assert.Equal(2, after.Version);
        Assert.Contains(":2:", after.Encrypt("new"));
        Assert.Equal(
            [1, 2],
            await scenario.Host.QueryAsync(db =>
                db.Users.AsNoTracking()
                    .Where(u => u.Id == scenario.DirectMemberUserId)
                    .SelectMany(u => u.SymmetricKeys!)
                    .Select(k => k.Version)
                    .OrderBy(g => g)
                    .ToListAsync()
            )
        );
    }

    [Fact]
    public async Task RotatingAUserSigningKey_KeepsIssuedSessionsValid()
    {
        var issuedBefore = await scenario.Host.WithScopeAsync(async services =>
        {
            var authentication = services.GetRequiredService<IAuthenticationService>();
            var signIn = await authentication.SignInAsync(
                new SignInRequest(scenario.GroupMemberUsername, IamScenario.Password, "before")
            );
            Assert.Equal(SignInResultCode.Success, signIn.ResultCode);

            var rotated = await services
                .GetRequiredService<IModificationService>()
                .RotateCurrentUserKeyAsync(KeyType.AsymmetricSignature);
            Assert.Equal(ModifyResultCode.Success, rotated.ResultCode);
            return signIn.AuthToken!;
        });

        var validation = await scenario.Host.WithScopeAsync(services =>
            services
                .GetRequiredService<IAuthenticationService>()
                .AuthenticateWithTokenAsync(issuedBefore)
        );

        Assert.Equal(UserAuthTokenValidationResultCode.Success, validation);
    }

    [Fact]
    public async Task RotatingAnAccountSigningKey_StillVerifiesEarlierSignatures()
    {
        var (signature, publicKeyBefore, after) = await scenario.ActAsAsync(
            scenario.OwnerUsername,
            scenario.OtherAccountId,
            async services =>
            {
                var retrieval = services.GetRequiredService<IRetrievalService>();
                var first = await retrieval.GetAccountAsymmetricSignatureProviderAsync(
                    scenario.OtherAccountId
                );
                var signed = first.Item!.Sign("payload");

                var rotated = await services
                    .GetRequiredService<IModificationService>()
                    .RotateAccountKeyAsync(
                        new RotateAccountKeyRequest(
                            scenario.OtherAccountId,
                            KeyType.AsymmetricSignature
                        )
                    );
                Assert.Equal(ModifyResultCode.Success, rotated.ResultCode);

                var second = await retrieval.GetAccountAsymmetricSignatureProviderAsync(
                    scenario.OtherAccountId
                );
                return (signed, first.Item.PublicKey, second.Item!);
            }
        );

        Assert.True(after.Verify("payload", signature));
        Assert.NotEqual(publicKeyBefore, after.PublicKey);
    }

    [Fact]
    public async Task RotatingUserKeys_IsRefused_ForSystemContext()
    {
        var result = await scenario.AsSystemAsync(services =>
            services
                .GetRequiredService<IModificationService>()
                .RotateCurrentUserKeyAsync(KeyType.SymmetricEncryption)
        );

        Assert.Equal(ModifyResultCode.UnauthorizedError, result.ResultCode);
    }

    [Fact]
    public async Task RotatingAccountKeys_IsRefused_ForUserOutsideTheAccount()
    {
        var result = await scenario.ActAsAsync(
            scenario.OutsiderUsername,
            null,
            services =>
                services
                    .GetRequiredService<IModificationService>()
                    .RotateAccountKeyAsync(
                        new RotateAccountKeyRequest(scenario.AccountId, KeyType.SymmetricEncryption)
                    )
        );

        Assert.Equal(ModifyResultCode.UnauthorizedError, result.ResultCode);
    }
}
