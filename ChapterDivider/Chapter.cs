using System.Collections.Generic;

namespace ChapterDivider
{
    internal class Chapter
    {
        public Chapter(string firstLine)
        {
            string[] split = firstLine.Split(':');
            int chapterNumber;

            int.TryParse(split[0].Remove(0, 8), out chapterNumber);

            Title = split[1].Trim();
            Number = chapterNumber;
        }

        private List<string> _Lines;
        internal string CompleteTitle { get { return "Chapitre " + Number + ": " + Title; } }
        internal List<string> Lines { get { return _Lines ?? (_Lines = new List<string>()); } }
        internal int Number { get; }
        internal string Title { get; }
    }
}