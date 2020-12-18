using System.Text.RegularExpressions;

namespace CrawlDown
{
    public static class StringExtensions
    {

        /// <summary>
        /// Removes excessive line breaks from a string.
        /// </summary>
        /// 
        /// <param name="s">
        /// A string which may contain line breaks.
        /// </param>
        /// 
        /// <returns>
        /// A string containing no more than 2 consecutive line breaks.
        /// </returns>
        public static string RemoveMultipleBlankLines(string s)
        {
            var regex = new Regex("(\\r?\\n){3,}");
            var result = regex.Replace(s, "\r\n\r\n");
            return result;
        }

    }
}
