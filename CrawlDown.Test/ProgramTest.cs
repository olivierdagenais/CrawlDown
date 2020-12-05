using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace CrawlDown
{
    /// <summary>
    /// A class to test <see cref="Program"/>.
    /// </summary>
    [TestClass]
    public class ProgramTest
    {
        private WireMockServer _server = null;
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
        public void StartWireMock()
        {
            _server = WireMockServer.Start();
            _baseUri = new UriBuilder("http", "localhost", _server.Ports[0]).Uri;

            // TODO: we only need to scan the resources folder once...
            FindResourceFiles();

            foreach (var keyTuplePair in _resources)
            {
                var relativePath = keyTuplePair.Key;
                var tuple = keyTuplePair.Value;
                var fileInfo = tuple.Item2;
                _server
                    .Given(
                        Request.Create()
                        .WithPath($"/{relativePath}")
                        .UsingGet()
                    )
                    .RespondWith(
                        Response.Create()
                            .WithStatusCode(200)
                            .WithBodyFromFile(fileInfo.FullName)
                    )
                ;
            }
        }

        [TestCleanup]
        public void StopWireMock()
        {
            _server.Stop();
            _server = null;
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
        public void RelativizePath_Incompatible()
        {
            var commonPath = @"C:\Users\user\src\project\repo";
            var longerPath = @"C:\Users\developer\src\project\repo\sub";

            var e = Assert.ThrowsException<ArgumentException>(() => { 
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
    }
}
