using Corely.Common.Filtering;
using Corely.Common.Filtering.Ordering;

namespace Corely.IAM.ApiKeys.Models;

public record ListApiKeysRequest(
    Guid AccountId,
    FilterBuilder<ApiKey>? Filter = null,
    OrderBuilder<ApiKey>? Order = null,
    int Skip = 0,
    int Take = 25
);
