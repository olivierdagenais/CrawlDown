﻿using SmartReader;
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

        // ReSharper disable once InconsistentNaming
        internal bool _isDebug = false;
        // ReSharper disable once InconsistentNaming
        internal string _destinationPath;

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
                if (SourceToImageMap.ContainsKey(imageUriString)) continue;

                var imageUriPath = imageUri.AbsolutePath.TrimEnd('/');
                var imageFileName = Path.GetFileName(imageUriPath);
                var imageFilePath = Path.Combine(_destinationPath, imageFileName);
                var imageFileInfo = new FileInfo(imageFilePath);
                var t = HttpClient.GetStreamAsync(imageUri).ContinueWith(innerTask =>
                {
                    if (innerTask.Status != TaskStatus.RanToCompletion) return;

                    using (var sw = new FileStream(imageFileInfo.FullName, FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        innerTask.Result.CopyTo(sw);
                    }
                    SourceToImageMap.Add(imageUriString, imageFileInfo);
                });
                tasks.Add(t);
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
            // ReSharper disable once ObjectCreationAsStatement
            new OfflineImage(converter, SourceToImageMap);

            var markdownText = converter.Convert(htmlString);

            var trimmedMarkdown = markdownText.Trim();
            // MD012/no-multiple-blanks
            var normalizedMarkdown = trimmedMarkdown.RemoveMultipleBlankLines();
            destination.WriteLine($"# {title}");
            destination.WriteLine();
            destination.Write(normalizedMarkdown);
            destination.WriteLine();
        }

    }
}
