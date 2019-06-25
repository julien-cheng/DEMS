namespace Documents.Clients.Manager.Modules
{
    using iTextSharp.text;
    using iTextSharp.text.pdf;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    public class ManifestEntry
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Size { get; set; }
    }

    public class ManifestGenerator
    {
        public static Task Generate(Stream contentsOut, string packageName, List<ManifestEntry> manifestEntries)
        {
            // at the bottom of every page, we're going to add our text
            Document document = null;
            PdfWriter writer = null;

            try
            {
                // setup a reader for the stream
                //reader = new PdfReader(contentsIn);
                document = new Document();

                // and a writer for our output
                writer = PdfWriter.GetInstance(document, contentsOut);

                // start reading the document
                document.Open();

                // start a new page in our output stream
                document.NewPage();

                document.Add(new Paragraph($"Compliance Report for {packageName}"));

                Table table = new Table(3);

                // These are percentages for each of the columns. 
                // Currently the columns are for name, size, and date modified.
                table.SetWidths(new int[] { 60, 10, 30 });

                // Aligning the table to left of the page, because 
                table.SetAlignment("left");

                // If you don't set some cell padding the text is too close to the borders.
                table.Cellpadding = 2;

                // Makes the borders thiner.
                table.BorderWidth = .2F;

                // These are our header cells.  I'm not using a font set until later.
                table.AddCell("Name");
                table.AddCell("Size");
                table.AddCell("Folder");

                // setup and cache the font and content writer
                var bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);

                Font font = new Font(bf, 8, Font.NORMAL);

                foreach (var manifestEntry in manifestEntries)
                {
                    table.AddCell(new Phrase(manifestEntry.Name, font));
                    table.AddCell(new Phrase(manifestEntry.Size, font));
                    table.AddCell(new Phrase(manifestEntry.Path, font));
                }

                document.Add(table);
            }
            finally
            {
                // this janky iText port doesn't support IDisposable
                try { document.Close(); } catch (Exception) { }
                try { writer.Close(); } catch (Exception) { }
            }

            return Task.FromResult(0);
        }
    }
}
