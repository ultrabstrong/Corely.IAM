using Corely.IAM.Security.Entities;
using Corely.IAM.Security.Enums;

namespace Corely.IAM.Security.Mappers;

internal static class KeyEntityExtensions
{
    extension<TKey>(IEnumerable<TKey>? keys)
        where TKey : class, IKeyEntity
    {
        public List<TKey> Versions(KeyUsedFor keyUsedFor) =>
            keys?.Where(k => k.KeyUsedFor == keyUsedFor).OrderBy(k => k.Version).ToList() ?? [];

        public TKey? CurrentVersion(KeyUsedFor keyUsedFor) =>
            keys.Versions(keyUsedFor).LastOrDefault();

        public int NextVersion(KeyUsedFor keyUsedFor) =>
            (keys.CurrentVersion(keyUsedFor)?.Version ?? 0) + 1;
    }
}
