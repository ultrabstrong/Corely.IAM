namespace Corely.IAM.Entities;

internal interface IAccountOwnedEntity
{
    Guid Id { get; }
    Guid AccountId { get; }
}
