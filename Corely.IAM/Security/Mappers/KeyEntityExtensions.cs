using Corely.IAM.Security.Entities;
using Corely.IAM.Security.Enums;

namespace Corely.IAM.Security.Mappers;

internal static class KeyEntityExtensions
{
    extension<TKey>(IEnumerable<TKey>? keys)
        where TKey : class, IKeyEntity
    {
        public List<TKey> Generations(KeyUsedFor keyUsedFor) =>
            keys?.Where(k => k.KeyUsedFor == keyUsedFor).OrderBy(k => k.Generation).ToList() ?? [];

        public TKey? CurrentGeneration(KeyUsedFor keyUsedFor) =>
            keys.Generations(keyUsedFor).LastOrDefault();

        public int NextGeneration(KeyUsedFor keyUsedFor) =>
            (keys.CurrentGeneration(keyUsedFor)?.Generation ?? 0) + 1;
    }
}
