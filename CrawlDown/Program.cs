using SmartReader;
using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("CrawlDown.Test")]

namespace CrawlDown
{
    public class Program
    {
        internal bool _isDebug = false;

        public Article Article {
            get;
            private set;
        }

        public static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }

        internal static string RelativizePath(string commonPath, string longerPath)
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

        public Article DownloadArticle(Uri uri)
        {
            var sr = new Reader(uri.ToString())
            {
                Debug = _isDebug,
            };
            if (_isDebug)
            {
                sr.LoggerDelegate = Console.WriteLine;
            }

            Article = sr.GetArticle();
            return Article;
        }
    }
}
