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
