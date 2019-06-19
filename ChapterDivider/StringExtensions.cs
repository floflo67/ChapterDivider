using System;
using System.IO;
using System.Text.RegularExpressions;

namespace ChapterDivider
{
    internal static class StringExtensions
    {
        public static Stream GenerateStream(this string input)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(input);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public static string StripHTML(this string input)
        {
            return Regex.Replace(input, "<.*?>", String.Empty);
        }
    }
}