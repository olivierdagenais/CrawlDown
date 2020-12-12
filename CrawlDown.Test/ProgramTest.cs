using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using MimeMapping;
using RichardSzalay.MockHttp;
using SmartReader;

namespace CrawlDown
{
    /// <summary>
    /// A class to test <see cref="Program"/>.
    /// </summary>
    [TestClass]
    public class ProgramTest
    {
        private MockHttpMessageHandler _mockHttp = null;
        private Uri _baseUri = null;
        private IDictionary<string, Tuple<Uri, FileInfo>> _resources = null;

        public void FindResourceFiles()
        {
            _resources = new Dictionary<string, Tuple<Uri, FileInfo>>();
            var resourcesFolder = Path.Combine(Environment.CurrentDirectory, "resources");
            var resourcesDirectory = new DirectoryInfo(resourcesFolder);
            var resourceFiles = resourcesDirectory.EnumerateFiles("*.*", SearchOption.AllDirectories);
            foreach (var resourceFile in resourceFiles)
            {
                var fixedRelativePath = Program.RelativizePath(resourcesFolder, resourceFile.FullName);
                var uri = new Uri(_baseUri, fixedRelativePath);
                _resources.Add(fixedRelativePath, Tuple.Create(uri, resourceFile));
            }
        }

        [TestInitialize]
        public void StartHttpMocking()
        {
            _mockHttp = new MockHttpMessageHandler();
            Reader.SetBaseHttpClientHandler(_mockHttp);
            Program.HttpClient = new HttpClient(_mockHttp);
            _baseUri = new UriBuilder("http", "localhost", 80).Uri;

            // TODO: we only need to scan the resources folder once...
            FindResourceFiles();

            foreach (var keyTuplePair in _resources)
            {
                var tuple = keyTuplePair.Value;
                var uri = tuple.Item1;
                var fileInfo = tuple.Item2;
                var contentType = MimeUtility.GetMimeMapping(fileInfo.Name);
                _mockHttp.When(uri.ToString())
                    .Respond(contentType, File.OpenRead(fileInfo.FullName));
            }
        }

        [TestCleanup]
        public void StopHttpMocking()
        {
            Reader.SetBaseHttpClientHandler(new HttpClientHandler());
            Program.HttpClient = new HttpClient();
            _mockHttp.Dispose();
            _mockHttp = null;
        }

        [TestMethod]
        public void HttpMocking_SupportsHeadMethodWithContentLength()
        {
            var testUri = UriForResource("WEWLC.html");
            var client = new HttpClient(_mockHttp);
            var request = new HttpRequestMessage(HttpMethod.Head, testUri);

            var task = client.SendAsync(request);
            task.Wait();
            var response = task.Result;

            Assert.AreEqual(50165, response.Content.Headers.ContentLength);
        }

        [TestMethod]
        public void Main()
        {
            Program.Main(new string[] { "" });
        }

        protected Uri UriForResource(string relativePath)
        {
            Assert.IsTrue(
                _resources.ContainsKey(relativePath),
                $"The path '{relativePath}' was not registered."
            );
            var tuple = _resources[relativePath];
            var uri = tuple.Item1;
            return uri;
        }

        [TestMethod]
        public void ConvertToMarkdown_Wewlc()
        {
            const string title = "Working Effectively With Legacy Code";
            var resource = _resources["WEWLC.html"];
            var sourceFileInfo = resource.Item2;
            using var sr = sourceFileInfo.OpenText();
            var htmlString = sr.ReadToEnd();
            var cut = new Program();
            using var destination = new StringWriter();

            cut.ConvertToMarkdown(title, htmlString, destination);

            var actual = destination.ToString();
            StringAssert.StartsWith(actual,
                @"# Working Effectively With Legacy Code

# [olivierdagenais.github.io](http://olivierdagenais.github.io/)

# Working Effectively With Legacy Code

A book summary by Olivier Dagenais

## Preface

> ");
        }

        [TestMethod]
        public void DownloadArticle_Wewlc()
        {
            var testUri = UriForResource("WEWLC.html");
            var cut = new Program();

            var actual = cut.DownloadArticle(testUri);

            Assert.AreEqual("Working Effectively With Legacy Code", actual.Title);
            var relativeDestinationPath = Program.RelativizePath(cut._destinationPath);
            Assert.AreEqual("localhost", relativeDestinationPath);
        }

        [TestMethod]
        public void DownloadImages_withImages()
        {
            var testUri = UriForResource("withImages/page.html");
            var cut = new Program();
            var article = cut.DownloadArticle(testUri);
            Assert.AreEqual("withImages", article.Title);

            cut.DownloadImages();

            var actual = cut.SourceToImageMap;
            Assert.AreEqual(1, actual.Count);
        }

        [TestMethod]
        public void RelativizePath_Incompatible()
        {
            var commonPath = @"C:\Users\user\src\project\repo";
            var longerPath = @"C:\Users\developer\src\project\repo\sub";

            var e = Assert.ThrowsException<ArgumentException>(() =>
            {
                Program.RelativizePath(commonPath, longerPath);
            });

            StringAssert.Contains(e.Message, @"repo\sub' does not start with 'C:\Users\user");
        }

        [TestMethod]
        public void RelativizePath_OneSubFolder()
        {
            var commonPath = @"C:\Users\developer\src\project\repo";
            var longerPath = @"C:\Users\developer\src\project\repo\sub";

            var actual = Program.RelativizePath(commonPath, longerPath);

            Assert.AreEqual("sub", actual);
        }

        [TestMethod]
        public void RelativizePath_TwoSubFolders()
        {
            var commonPath = @"C:\Users\developer\src\project\repo";
            var longerPath = @"C:\Users\developer\src\project\repo\sub\folder";

            var actual = Program.RelativizePath(commonPath, longerPath);

            Assert.AreEqual("sub/folder", actual);
        }

        [TestMethod]
        public void RemoveMultipleBlankLines_EmptyString()
        {
            const string input = @"";

            var actual = Program.RemoveMultipleBlankLines(input);

            Assert.AreEqual("", actual);
        }

        [TestMethod]
        public void RemoveMultipleBlankLines_OneLine()
        {
            const string input = @"The quick brown fox jumps over the lazy dog's back.";

            var actual = Program.RemoveMultipleBlankLines(input);

            Assert.AreEqual("The quick brown fox jumps over the lazy dog's back.", actual);
        }

        [TestMethod]
        public void RemoveMultipleBlankLines_OneTwoThreeFourFive()
        {
            const string input = @"The
quick

brown


fox



jumps




over the lazy dog's back.";

            var actual = Program.RemoveMultipleBlankLines(input);

            const string expected = @"The
quick

brown

fox

jumps

over the lazy dog's back.";
            Assert.AreEqual(expected, actual);
        }
    }
}
