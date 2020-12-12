using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using ReverseMarkdown;
using ReverseMarkdown.Converters;

namespace CrawlDown
{
    /// <summary>
    /// This class was ~stolen~ adapted from <see cref="Img"/>, except for the
    /// <see cref="EscapeLinkText(string)"/> method, which wasn't visible, so it was
    /// also imported.
    /// </summary>
    public class OfflineImage : Img
    {
        private readonly IDictionary<string, FileInfo> _sourceToImageMap;

        public OfflineImage(Converter converter, IDictionary<string, FileInfo> sourceToImageMap)
            : base(converter)
        {
            Converter.Register("img", this);
            _sourceToImageMap = sourceToImageMap;
        }

        public override string Convert(HtmlNode node)
        {
            var result = base.Convert(node);
            if (result.Length <= 0) return result;

            var src = node.GetAttributeValue("src", string.Empty);
            if (_sourceToImageMap.ContainsKey(src))
            {
                var imageFileInfo = _sourceToImageMap[src];
                src = imageFileInfo.Name;
            }
            var alt = node.GetAttributeValue("alt", src);
            var title = ExtractTitle(node);
            title = title.Length > 0 ? $" \"{title}\"" : "";

            result = $"![{EscapeLinkText(alt)}]({src}{title})";
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
