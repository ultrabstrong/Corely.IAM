using Corely.IAM.DataAccess;
using Corely.IAM.IntegrationTests.Infrastructure;
using Corely.IAM.Permissions.Entities;
using Corely.IAM.Permissions.Processors;
using Corely.IAM.Roles.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.IntegrationTests.Authorization;

public class MissingOwnerDefaultsTests : IAsyncLifetime
{
    private readonly IamScenario _scenario = new();

    public ValueTask InitializeAsync() => _scenario.InitializeAsync();

    public ValueTask DisposeAsync() => _scenario.DisposeAsync();

    [Fact]
    public async Task CreateMissingOwnerDefaults_RecreatesAMissingRow_OnlyWhereItIsMissing()
    {
        await _scenario.Host.QueryAsync(async db =>
        {
            var invoice = await InvoiceOwnerDefaultAsync(db, _scenario.AccountId);
            invoice.Roles!.Clear();
            db.Remove(invoice);
            return await db.SaveChangesAsync();
        });

        var added = await CreateMissingOwnerDefaultsAsync();
        var again = await CreateMissingOwnerDefaultsAsync();

        Assert.Equal(1, added);
        Assert.Equal(0, again);
        var recreated = await _scenario.Host.QueryAsync(db =>
            InvoiceOwnerDefaultAsync(db, _scenario.AccountId)
        );
        Assert.True(recreated.IsSystemDefined);
        Assert.Contains(recreated.Roles!, r => r.Name == RoleConstants.OWNER_ROLE_NAME);
    }

    [Fact]
    public async Task CreateMissingOwnerDefaults_AdoptsAnIdenticalOrdinaryRow()
    {
        var id = await _scenario.Host.QueryAsync(async db =>
        {
            var invoice = await InvoiceOwnerDefaultAsync(db, _scenario.AccountId);
            invoice.Roles!.Clear();
            invoice.IsSystemDefined = false;
            await db.SaveChangesAsync();
            return invoice.Id;
        });

        var added = await CreateMissingOwnerDefaultsAsync();

        Assert.Equal(1, added);
        var adopted = await _scenario.Host.QueryAsync(db =>
            InvoiceOwnerDefaultAsync(db, _scenario.AccountId)
        );
        Assert.Equal(id, adopted.Id);
        Assert.True(adopted.IsSystemDefined);
        Assert.Contains(adopted.Roles!, r => r.Name == RoleConstants.OWNER_ROLE_NAME);
    }

    private Task<int> CreateMissingOwnerDefaultsAsync() =>
        _scenario.Host.WithScopeAsync(services =>
            services.GetRequiredService<IPermissionProcessor>().CreateMissingOwnerDefaultsAsync()
        );

    private static Task<PermissionEntity> InvoiceOwnerDefaultAsync(
        IamDbContext db,
        Guid accountId
    ) =>
        db.Set<PermissionEntity>()
            .Include(p => p.Roles)
            .SingleAsync(p =>
                p.AccountId == accountId
                && p.ResourceType == IamScenario.INVOICE_RESOURCE_TYPE
                && p.ResourceId == Guid.Empty
                && p.Read
                && !p.Create
                && !p.Update
                && !p.Delete
                && !p.Execute
            );
}
