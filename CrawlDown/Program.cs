using SmartReader;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("CrawlDown.Test")]

namespace CrawlDown
{
    public class Program
    {
        internal bool _isDebug = false;
        internal string _destinationPath = null;

        public Article Article {
            get;
            private set;
        }

        public DirectoryInfo DestinationRoot
        {
            get;
            set;
        } = new DirectoryInfo(Environment.CurrentDirectory);

        public static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }

        internal static string RelativizePath(string longerPath)
        {
            return RelativizePath(Environment.CurrentDirectory, longerPath);
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
                sr.LoggerDelegate = message =>
                {
                    Debug.WriteLine(message);
                };
            }

            Article = sr.GetArticle();
            _destinationPath = Path.Combine(DestinationRoot.FullName, uri.Host);
            return Article;
        }
    }
}
