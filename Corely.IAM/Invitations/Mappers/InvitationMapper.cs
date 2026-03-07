using Corely.IAM.Invitations.Entities;
using Corely.IAM.Invitations.Models;

namespace Corely.IAM.Invitations.Mappers;

internal static class InvitationMapper
{
    public static Invitation ToModel(this InvitationEntity entity)
    {
        return new Invitation
        {
            Id = entity.Id,
            AccountId = entity.AccountId,
            CreatedByUserId = entity.CreatedByUserId,
            Email = entity.Email,
            Description = entity.Description,
            ExpiresUtc = entity.ExpiresUtc,
            AcceptedByUserId = entity.AcceptedByUserId,
            AcceptedUtc = entity.AcceptedUtc,
            RevokedUtc = entity.RevokedUtc,
            CreatedUtc = entity.CreatedUtc,
        };
    }
}
