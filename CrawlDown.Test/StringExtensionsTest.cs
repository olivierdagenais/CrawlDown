using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrawlDown
{
    /// <summary>
    /// A class to test <see cref="StringExtensions"/>.
    /// </summary>
    [TestClass]
    public class StringExtensionsTest
    {

        [TestMethod]
        public void RemoveMultipleBlankLines_EmptyString()
        {
            const string input = @"";

            var actual = StringExtensions.RemoveMultipleBlankLines(input);

            Assert.AreEqual("", actual);
        }

        [TestMethod]
        public void RemoveMultipleBlankLines_OneLine()
        {
            const string input = @"The quick brown fox jumps over the lazy dog's back.";

            var actual = StringExtensions.RemoveMultipleBlankLines(input);

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

            var actual = StringExtensions.RemoveMultipleBlankLines(input);

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
