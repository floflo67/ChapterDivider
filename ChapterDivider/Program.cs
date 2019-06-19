using HtmlAgilityPack;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace ChapterDivider
{
    internal class Program
    {
        private const string _baseUrl = @"https://www.webnovel.com/book/11297889706318205/";
        private const string _FilePath = @"D:\Downloads\temp\Tempest of the Stellar War.txt";
        private const string _NovelName = "Tempest of the Stellar War";
        private const string _SavePath = @"D:\Downloads\temp\Tempest of the Stellar War\";

        private static readonly DivideParameters parameters = new DivideParameters()
        {
            ShouldAddDarkBackground = true,
            ShouldCreateMultiplePdf = true,
            ShouldGetSingleChapter = false,
        };

        private static List<Chapter> _Chapters;
        private static long _currentChapterId = 30997799382337779;
        private static List<Chapter> Chapters { get { return _Chapters ?? (_Chapters = new List<Chapter>()); } }

        public static void Main(string[] args)
        {
            GetChaptersFromExistingFile();
            //GetChaptersFromUrl();
            CreatePdfUsingChapters();
        }

        private static void AddChapterFromUrl(long currentChapterId, out long nextChapterId)
        {
            HtmlWeb web = new HtmlWeb();
            var htmlDoc = web.Load(_baseUrl + currentChapterId);
            var htmlCode = htmlDoc.ParsedText;

            string description = htmlDoc.DocumentNode.SelectSingleNode(".//*[contains(@class,'chapter_content')]").InnerText;
            description = HttpUtility.HtmlDecode(description.StripHTML());

            var startSearchId = htmlCode.IndexOf("g_data.nextcId");
            var endSearchId = htmlCode.IndexOf(';', startSearchId);

            var s = htmlCode.Substring(startSearchId, endSearchId - startSearchId);
            nextChapterId = long.Parse(s.Split('\'')[1]);

            var stream = description.GenerateStream();
            CreateChapterFromStream(stream);
        }

        private static void AddDarkBackgroundColor(string filename)
        {
            PdfDocument doc = PdfReader.Open(_SavePath + filename);
            doc.Options.ColorMode = PdfColorMode.Cmyk;
            foreach (var page in doc.Pages)
            {
                XGraphics gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);
                gfx.DrawRectangle(new XSolidBrush(XColor.FromCmyk(0.4, 0.25, 0.10, 0, 1)), new XRect(0, 0, 1000, 1000));
            }

            doc.Save(_SavePath + filename);
        }

        private static void CreateChapterFromStream(Stream fileStream)
        {
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
            {
                string line;
                Chapter chap = null;
                while ((line = streamReader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if ((line.StartsWith("Translator: ") || line.StartsWith("Editor: ")) && chap != null) { }
                    else if (line.StartsWith("Chapter ") && chap == null)
                    {
                        chap = new Chapter(line);
                    }
                    else if (line.StartsWith("Comments ") && chap != null)
                    {
                        Chapters.Add(chap);
                        chap = null;
                    }
                    else if (chap != null && !string.IsNullOrWhiteSpace(line)) { chap.Lines.Add(line); }
                }
            }
        }

        private static void CreatePdfUsingChapters()
        {
            Console.WriteLine("Chapter count : " + Chapters.Count);

            if (parameters.ShouldCreateMultiplePdf.HasValue && parameters.ShouldCreateMultiplePdf.Value == true)
            {
                var filenames = RenderMultiplePdf();

                if (parameters.ShouldAddDarkBackground.HasValue && parameters.ShouldAddDarkBackground.Value == true)
                {
                    foreach (var s in filenames) { AddDarkBackgroundColor(s); }
                }
            }
            else
            {
                string filename = RenderOnePdf();

                if (parameters.ShouldAddDarkBackground.HasValue && parameters.ShouldAddDarkBackground.Value == true)
                {
                    AddDarkBackgroundColor(filename);
                }
            }
        }

        private static void GetChaptersFromExistingFile()
        {
            var fileStream = new FileStream(_FilePath, FileMode.Open, FileAccess.Read);
            CreateChapterFromStream(fileStream);
        }

        private static void GetChaptersFromUrl()
        {
            long nextChapterId = 0;

            // 23413400386206370
            // == -1 if end of chapters
            if (parameters.ShouldGetSingleChapter.HasValue && parameters.ShouldGetSingleChapter.Value)
            {
                AddChapterFromUrl(_currentChapterId, out nextChapterId);
            }
            else
            {
                while (nextChapterId >= 0)
                {
                    AddChapterFromUrl(_currentChapterId, out nextChapterId);
                    _currentChapterId = nextChapterId;
                    Console.WriteLine($"Add chapter {_currentChapterId}, working on chapter {nextChapterId}");
                }
            }
        }

        private static IEnumerable<string> RenderMultiplePdf()
        {
            int chapDone = 1;
            int maxChap = Chapters.Count;
            List<string> fileNames = new List<string>();

            foreach (var c in Chapters)
            {
                PdfDocumentRenderer pdfRenderer = new PdfDocumentRenderer(false);
                Document document = new Document();
                var style = document.Styles["Normal"];
                style.Font.Size = 14;
                Section section = document.AddSection();
                Paragraph paragraph = section.AddParagraph();
                paragraph.Format.Alignment = ParagraphAlignment.Center;
                FormattedText ft = paragraph.AddFormattedText(c.CompleteTitle, TextFormat.Bold);
                ft.Font.Size = 18;
                paragraph.AddLineBreak();

                foreach (var l in c.Lines)
                {
                    section.AddParagraph(l);
                }

                pdfRenderer.Document = document;
                pdfRenderer.RenderDocument();

                string filename = $"{_NovelName} {c.Number}.pdf";

                if (!Directory.Exists(_SavePath))
                {
                    Directory.CreateDirectory(_SavePath);
                }

                pdfRenderer.PdfDocument.Save(_SavePath + filename);
                fileNames.Add(filename);
                if (chapDone % 10 == 0)
                {
                    Console.WriteLine("Processed " + chapDone + "/" + maxChap);
                }
                document = null;
                pdfRenderer = null;
                chapDone++;
            }

            return fileNames;
        }

        private static string RenderOnePdf()
        {
            int chapDone = 1;
            int maxChap = Chapters.Count;
            PdfDocumentRenderer pdfRenderer = new PdfDocumentRenderer(false);
            Document document = new Document();
            var style = document.Styles["Normal"];
            style.Font.Size = 14;

            foreach (var c in Chapters.Where(x => x.Number < 100))
            {
                Section section = document.AddSection();
                Paragraph paragraph = section.AddParagraph();
                paragraph.Format.Alignment = ParagraphAlignment.Center;
                FormattedText ft = paragraph.AddFormattedText(c.CompleteTitle, TextFormat.Bold);
                ft.Font.Size = 18;
                paragraph.AddLineBreak();

                foreach (var l in c.Lines)
                {
                    section.AddParagraph(l);
                }

                if (chapDone % 10 == 0)
                {
                    Console.WriteLine("Processed " + chapDone + "/" + maxChap);
                }

                chapDone++;
            }

            pdfRenderer.Document = document;
            pdfRenderer.RenderDocument();
            string filename = $"{_NovelName}.pdf";

            if (!Directory.Exists(_SavePath))
            {
                Directory.CreateDirectory(_SavePath);
            }

            pdfRenderer.PdfDocument.Save(_SavePath + filename);
            return filename;
        }
    }
}