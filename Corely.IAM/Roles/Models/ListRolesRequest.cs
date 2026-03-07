using Corely.Common.Filtering;
using Corely.Common.Filtering.Ordering;

namespace Corely.IAM.Roles.Models;

public record ListRolesRequest(
    FilterBuilder<Role>? Filter = null,
    OrderBuilder<Role>? Order = null,
    int Skip = 0,
    int Take = 25
);
