using System;
using System.Text.RegularExpressions;

namespace ChapterDivider
{
    internal static class StringExtensions
    {
        public static string StripHTML(this string input)
        {
            return Regex.Replace(input, "<.*?>", String.Empty);
        }
    }
}