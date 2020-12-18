using System;

namespace CrawlDown
{
    public static class PathExtensions
    {
        public static string RelativizePath(this string longerPath)
        {
            return RelativizePath(Environment.CurrentDirectory, longerPath);
        }

        public static string RelativizePath(this string commonPath, string longerPath)
        {
            if (!longerPath.StartsWith(commonPath))
            {
                throw new ArgumentException(
                    $"{nameof(longerPath)} '{longerPath}' does not start with '{commonPath}'",
                    nameof(longerPath)
                );
            }
            var relativePath = longerPath.Substring(commonPath.Length + 1);
            var result = relativePath.Replace('\\', '/');
            return result;
        }


    }
}
