using Microsoft.AspNetCore.Http;

namespace Corely.IAM.Web.Extensions;

internal static class PathStringExtensions
{
    private static readonly HashSet<string> CacheableStaticExtensions =
    [
        ".css",
        ".js",
        ".map",
        ".png",
        ".jpg",
        ".jpeg",
        ".gif",
        ".svg",
        ".ico",
        ".webp",
        ".woff",
        ".woff2",
        ".ttf",
        ".eot",
        ".webmanifest",
    ];

    extension(PathString path)
    {
        public bool IsCacheableStaticAsset()
        {
            if (!path.HasValue)
            {
                return false;
            }

            var pathValue = path.Value!;
            if (
                pathValue.StartsWith("/_framework", StringComparison.OrdinalIgnoreCase)
                || pathValue.StartsWith("/_content", StringComparison.OrdinalIgnoreCase)
            )
            {
                return true;
            }

            var extension = System.IO.Path.GetExtension(pathValue);
            return !string.IsNullOrWhiteSpace(extension)
                && CacheableStaticExtensions.Contains(extension);
        }
    }
}
