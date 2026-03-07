using Corely.Common.Filtering;
using Corely.Common.Filtering.Ordering;

namespace Corely.IAM.Groups.Models;

public record ListGroupsRequest(
    FilterBuilder<Group>? Filter = null,
    OrderBuilder<Group>? Order = null,
    int Skip = 0,
    int Take = 25
);
