using SmartReader;
using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("CrawlDown.Test")]

namespace CrawlDown
{
    public class Program
    {
        private bool _isDebug = false;

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

            var result = sr.GetArticle();
            return result;
        }
    }
}
