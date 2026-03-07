using Corely.Common.Filtering;
using Corely.Common.Filtering.Ordering;

namespace Corely.IAM.Users.Models;

public record ListUsersRequest(
    FilterBuilder<User>? Filter = null,
    OrderBuilder<User>? Order = null,
    int Skip = 0,
    int Take = 25
);
