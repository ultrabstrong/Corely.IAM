namespace Corely.IAM.Web.Extensions;

internal static class GuidArrayExtensions
{
    extension(Guid[]? ids)
    {
        public bool SameIdsAs(Guid[]? other)
        {
            if (ReferenceEquals(ids, other))
                return true;
            if (ids is null || other is null)
                return false;
            return ids.AsSpan().SequenceEqual(other);
        }
    }
}
