using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrawlDown
{
    /// <summary>
    /// A class to test <see cref="PathExtensions"/>.
    /// </summary>
    [TestClass]
    public class PathExtensionsTest
    {

        [TestMethod]
        public void RelativizePath_Incompatible()
        {
            var commonPath = @"C:\Users\user\src\project\repo";
            var longerPath = @"C:\Users\developer\src\project\repo\sub";

            var e = Assert.ThrowsException<ArgumentException>(() =>
            {
                PathExtensions.RelativizePath(commonPath, longerPath);
            });

            StringAssert.Contains(e.Message, @"repo\sub' does not start with 'C:\Users\user");
        }

        [TestMethod]
        public void RelativizePath_OneSubFolder()
        {
            var commonPath = @"C:\Users\developer\src\project\repo";
            var longerPath = @"C:\Users\developer\src\project\repo\sub";

            var actual = PathExtensions.RelativizePath(commonPath, longerPath);

            Assert.AreEqual("sub", actual);
        }

        [TestMethod]
        public void RelativizePath_TwoSubFolders()
        {
            var commonPath = @"C:\Users\developer\src\project\repo";
            var longerPath = @"C:\Users\developer\src\project\repo\sub\folder";

            var actual = PathExtensions.RelativizePath(commonPath, longerPath);

            Assert.AreEqual("sub/folder", actual);
        }

    }
}
