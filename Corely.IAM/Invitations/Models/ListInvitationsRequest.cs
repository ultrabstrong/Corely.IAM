using Corely.Common.Filtering;
using Corely.Common.Filtering.Ordering;

namespace Corely.IAM.Invitations.Models;

public record ListInvitationsRequest(
    Guid AccountId,
    FilterBuilder<Invitation>? Filter = null,
    OrderBuilder<Invitation>? Order = null,
    int Skip = 0,
    int Take = 25,
    InvitationStatus? StatusFilter = null
);
