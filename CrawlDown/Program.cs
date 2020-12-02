using HtmlAgilityPack;
using ReverseMarkdown;
using ReverseMarkdown.Converters;
using SmartReader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("CrawlDown.Test")]

namespace CrawlDown
{
    public class Program
    {
        private static readonly HttpClient httpClient = new HttpClient();
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var uriString = "https://arstechnica.com/information-technology/2017/02/humans-must-become-cyborgs-to-survive-says-elon-musk/";
            var destination = new DirectoryInfo("Incoming/");

            var uri = new Uri(uriString);
            var sr = new Reader(uriString)
            {
                Debug = true,
                LoggerDelegate = Console.WriteLine
            };

            var article = sr.GetArticle();
            var urlPath = uri.AbsolutePath.TrimEnd('/');
            var urlFileName = Path.GetFileName(urlPath);
            var destinationPath = Path.Combine(destination.FullName, uri.Host);
            Directory.CreateDirectory(destinationPath);

            var task = article.GetImagesAsync(0);
            task.Wait();
            var images = task.Result;
            var sourceToImageMap = new Dictionary<string, FileInfo>();
            var tasks = new List<Task>();
            foreach (var image in images)
            {
                var imageUri = image.Source;
                var imageUriString = imageUri.ToString();
                if (!sourceToImageMap.ContainsKey(imageUriString))
                {
                    var imageUriPath = imageUri.AbsolutePath.TrimEnd('/');
                    var imageFileName = Path.GetFileName(imageUriPath);
                    var imageFilePath = Path.Combine(destinationPath, imageFileName);
                    var imageFileInfo = new FileInfo(imageFilePath);
                    var t = httpClient.GetStreamAsync(imageUri).ContinueWith(t =>
                    {
                        if (t.Status == TaskStatus.RanToCompletion)
                        {
                            using (var sw = new FileStream(imageFileInfo.FullName, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                            {
                                t.Result.CopyTo(sw);
                            }
                            sourceToImageMap.Add(imageUriString, imageFileInfo);
                        }
                    });
                    tasks.Add(t);
                }
            }
            Task.WaitAll(tasks.ToArray());

            var titleOrFile = article.Title ?? urlFileName;
            var fileName = Path.ChangeExtension(titleOrFile, ".md");
            var destinationFile = Path.Combine(destinationPath, fileName);

            var rmConfig = new Config
            {
                GithubFlavored = true,
                ListBulletChar = '*',
                RemoveComments = true,
                SmartHrefHandling = false,
                UnknownTags = Config.UnknownTagsOption.Bypass,
            };
            var converter = new Converter(rmConfig);
            var oi = new OfflineImage(converter, sourceToImageMap);
            #region DEBUG
            using (var sw = new StreamWriter(destinationFile + ".html", false, Encoding.UTF8))
            {
                sw.Write(article.Content);
            }
            #endregion
            var markdownText = converter.Convert(article.Content);
            // MD012/no-multiple-blanks
            var improvedMarkdownText = markdownText.Replace("\r\n\r\n\r\n", "\r\n\r\n").Trim();
            using (var sw = new StreamWriter(destinationFile, false, Encoding.UTF8))
            {
                sw.WriteLine($"# {titleOrFile}");
                sw.WriteLine();
                sw.Write(improvedMarkdownText);
                sw.WriteLine();
            }
        }
    }

    public class OfflineImage : Img
    {
        private readonly IDictionary<string, FileInfo> sourceToImageMap;
        public OfflineImage(Converter converter, IDictionary<string, FileInfo> sourceToImageMap) : base (converter)
        {
            Converter.Register("img", this);
            this.sourceToImageMap = sourceToImageMap;
        }

        public override string Convert(HtmlNode node)
        {
            var result = base.Convert(node);
            if ( result.Length > 0)
            {
                var src = node.GetAttributeValue("src", string.Empty);
                if (sourceToImageMap.ContainsKey(src)) {
                    var imageFileInfo = sourceToImageMap[src];
                    src = imageFileInfo.Name;
                }
                var alt = node.GetAttributeValue("alt", src);
                var title = ExtractTitle(node);
                title = title.Length > 0 ? $" \"{title}\"" : "";

                result = $"![{EscapeLinkText(alt)}]({src}{title})";
            }
            return result;
        }

        /// <summary>
        /// Escape/clean characters which would break the [] section of a markdown []() link
        /// </summary>
        internal static string EscapeLinkText(string rawText)
        {
            return Regex.Replace(rawText, @"\r?\n\s*\r?\n", Environment.NewLine, RegexOptions.Singleline)
                .Replace("[", @"\[")
                .Replace("]", @"\]");
        }
    }
}
