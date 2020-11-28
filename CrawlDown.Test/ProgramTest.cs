using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrawlDown
{
    /// <summary>
    /// A class to test <see cref="Program"/>.
    /// </summary>
    [TestClass]
    public class ProgramTest
    {
        [TestMethod]
        public void Main()
        {
            Program.Main(new string[] { "" });
        }
    }
}
