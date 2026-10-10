using Corely.IAM.Audits.Entities;
using Corely.IAM.Audits.Mappers;
using Corely.IAM.Audits.Models;
using Corely.IAM.Security.Constants;

namespace Corely.IAM.UnitTests.Audits.Mappers;

public class AuditEntryMapperTests
{
    [Fact]
    public void ToResourceIdsColumn_ReturnsNull_WhenThereAreNoIds()
    {
        Assert.Null(Array.Empty<Guid>().ToResourceIdsColumn());
        Assert.Null(new[] { Guid.Empty }.ToResourceIdsColumn());
    }

    [Fact]
    public void ToResourceIdsColumn_JoinsDistinctIds()
    {
        var first = Guid.CreateVersion7();
        var second = Guid.CreateVersion7();

        Assert.Equal($"{first},{second}", new[] { first, second, first }.ToResourceIdsColumn());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void ToResourceIdList_ReturnsEmpty_ForAnEmptyColumn(string? column)
    {
        Assert.Empty(column.ToResourceIdList());
    }

    [Fact]
    public void ToResourceIdList_ReadsBackWhatTheColumnHolds_AndSkipsGarbage()
    {
        var first = Guid.CreateVersion7();
        var second = Guid.CreateVersion7();

        Assert.Equal([first, second], $"{first},not-a-guid,,{second}".ToResourceIdList());
    }

    [Fact]
    public void ToModel_CopiesEveryColumn()
    {
        var resourceId = Guid.CreateVersion7();
        var entity = new AuditEntryEntity
        {
            Id = Guid.CreateVersion7(),
            OccurredUtc = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
            ActorUserId = Guid.CreateVersion7(),
            Cohort = AuditCohort.PlatformMember,
            AccountId = Guid.CreateVersion7(),
            Source = "portal",
            Service = "IRegistrationService",
            Operation = "RegisterGroupAsync",
            Action = AuthAction.Create,
            ResourceType = "group",
            ResourceIds = resourceId.ToString(),
            ResultCode = "Success",
            Details = "detail",
        };

        var model = entity.ToModel();

        Assert.Equal(
            new AuditEntry(
                entity.Id,
                entity.OccurredUtc,
                entity.ActorUserId,
                entity.Cohort,
                entity.AccountId,
                entity.Source,
                entity.Service,
                entity.Operation,
                entity.Action,
                entity.ResourceType,
                model.ResourceIds,
                entity.ResultCode,
                entity.Details
            ),
            model
        );
        Assert.Equal([resourceId], model.ResourceIds);
    }
}
