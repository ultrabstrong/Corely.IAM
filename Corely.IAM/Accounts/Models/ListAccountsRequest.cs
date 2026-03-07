using Corely.Common.Filtering;
using Corely.Common.Filtering.Ordering;

namespace Corely.IAM.Accounts.Models;

public record ListAccountsRequest(
    FilterBuilder<Account>? Filter = null,
    OrderBuilder<Account>? Order = null,
    int Skip = 0,
    int Take = 25
);
