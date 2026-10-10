using Corely.IAM.Audits.Models;
using Corely.IAM.Security.Constants;

namespace Corely.IAM.UnitTests.Audits.Models;

public class AuditEntryTests
{
    [Fact]
    public void CsvRow_HasOneFieldPerHeaderColumn_InOrder()
    {
        var resourceId = Guid.CreateVersion7();
        var entry = new AuditEntry(
            Guid.CreateVersion7(),
            new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
            null,
            AuditCohort.SystemContext,
            null,
            "portal",
            "IAuditService",
            "PurgeEntriesAsync",
            AuthAction.Delete,
            "audit",
            [resourceId],
            "Success",
            null
        )
        {
            AccountName = "Acme",
        };

        var fields = entry.CsvRow().Split(',');

        Assert.Equal(AuditEntry.CSV_HEADER.Split(',').Length, fields.Length);
        Assert.Equal(entry.Id.ToString(), fields[0]);
        Assert.Equal("SystemContext", fields[4]);
        Assert.Equal("Acme", fields[6]);
        Assert.Equal("Delete", fields[10]);
        Assert.Equal(resourceId.ToString(), fields[12]);
    }

    [Theory]
    [InlineData(AuditCohort.PlatformMember, true)]
    [InlineData(AuditCohort.AccountMember, false)]
    public void IsPlatformMember_ReadsTheCohort(AuditCohort cohort, bool expected)
    {
        var entry = new AuditEntry(
            Guid.CreateVersion7(),
            DateTime.UtcNow,
            null,
            cohort,
            null,
            "s",
            "svc",
            "op",
            AuthAction.Read,
            "audit",
            [],
            "Success",
            null
        );

        Assert.Equal(expected, entry.IsPlatformMember);
    }
}
