using SmartReader;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ReverseMarkdown;

[assembly: InternalsVisibleTo("CrawlDown.Test")]

namespace CrawlDown
{
    public class Program
    {
        internal static HttpClient HttpClient = new HttpClient();

        internal bool _isDebug = false;
        internal string _destinationPath = null;

        public Article Article
        {
            get;
            private set;
        }

        public DirectoryInfo DestinationRoot
        {
            get;
            set;
        } = new DirectoryInfo(Environment.CurrentDirectory);

        public IDictionary<string, FileInfo> SourceToImageMap
        {
            get;
        } = new Dictionary<string, FileInfo>();

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

        public void DownloadImages()
        {
            SourceToImageMap.Clear();
            var task = Article.GetImagesAsync(0);
            task.Wait();
            var images = task.Result;
            var tasks = new List<Task>();
            Directory.CreateDirectory(_destinationPath);
            foreach (var image in images)
            {
                var imageUri = image.Source;
                var imageUriString = imageUri.ToString();
                if (!SourceToImageMap.ContainsKey(imageUriString))
                {
                    var imageUriPath = imageUri.AbsolutePath.TrimEnd('/');
                    var imageFileName = Path.GetFileName(imageUriPath);
                    var imageFilePath = Path.Combine(_destinationPath, imageFileName);
                    var imageFileInfo = new FileInfo(imageFilePath);
                    var t = HttpClient.GetStreamAsync(imageUri).ContinueWith(t =>
                    {
                        if (t.Status == TaskStatus.RanToCompletion)
                        {
                            using (var sw = new FileStream(imageFileInfo.FullName, FileMode.Create, FileAccess.Write, FileShare.Read))
                            {
                                t.Result.CopyTo(sw);
                            }
                            SourceToImageMap.Add(imageUriString, imageFileInfo);
                        }
                    });
                    tasks.Add(t);
                }
            }
            Task.WaitAll(tasks.ToArray());
        }

        public void ConvertToMarkdown(string title, string htmlString, TextWriter destination)
        {
            var rmConfig = new Config
            {
                GithubFlavored = true,
                ListBulletChar = '*',
                RemoveComments = true,
                SmartHrefHandling = false,
                UnknownTags = Config.UnknownTagsOption.Bypass,
            };
            var converter = new Converter(rmConfig);

            var markdownText = converter.Convert(htmlString);

            var trimmedMarkdown = markdownText.Trim();
            destination.WriteLine($"# {title}");
            destination.WriteLine();
            destination.Write(trimmedMarkdown);
            destination.WriteLine();
        }

        internal static string RemoveMultipleBlankLines(string s)
        {
            var regex = new Regex("(\\r?\\n){3,}");
            var result = regex.Replace(s, "\r\n\r\n");
            return result;
        }
    }
}
