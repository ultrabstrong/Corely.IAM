using Corely.Common.Filtering;
using Corely.Common.Filtering.Ordering;

namespace Corely.IAM.Permissions.Models;

public record ListPermissionsRequest(
    Guid AccountId,
    FilterBuilder<Permission>? Filter = null,
    OrderBuilder<Permission>? Order = null,
    int Skip = 0,
    int Take = 25
);
