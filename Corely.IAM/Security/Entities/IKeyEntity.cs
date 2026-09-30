using Corely.IAM.Security.Enums;

namespace Corely.IAM.Security.Entities;

internal interface IKeyEntity
{
    KeyUsedFor KeyUsedFor { get; }
    int Version { get; }
}
