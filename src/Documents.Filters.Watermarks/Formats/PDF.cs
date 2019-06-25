namespace Documents.Filters.Watermarks
{
    using System;
    using iTextSharp.text;
    using iTextSharp.text.pdf;
    using System.IO;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    public class PDF : IWatermarker
    {
        async Task IWatermarker.Watermark(Stream contentsIn, Stream contentsOut, string text)
        {
            // at the bottom of every page, we're going to add our text

            PdfReader reader = null;
            Document document = null;
            PdfWriter writer = null;

            using (var memoryBufferIn = new MemoryStream())
            using (var memoryBufferOut = new MemoryStream())
            {
                try
                {
                    await contentsIn.CopyToAsync(memoryBufferIn);
                    memoryBufferIn.Seek(0, SeekOrigin.Begin);

                    // setup a reader for the stream
                    reader = new PdfReader(memoryBufferIn);
                    document = new Document(reader.GetPageSizeWithRotation(1));

                    // and a writer for our output
                    writer = PdfWriter.GetInstance(document, memoryBufferOut);

                    // connect the internal calss EventHelper (below) to the PageEvent property
                    // this will let it handle events per-page and do the actual drawing of the text footers
                    writer.PageEvent = new EventHelper(text);

                    // start reading the document
                    document.Open();

                    // loop the pages (pdf page numbers are 1-based)
                    for (var pageNumber = 1; pageNumber <= reader.NumberOfPages; pageNumber++)
                    {

                        // start a new page in our output stream
                        document.NewPage();
                        // clone the page size from the original page (hopefully this deals properly with variable page sizes)
                        var rect = reader.GetPageSizeWithRotation(pageNumber);
                        document.SetPageSize(rect);

                        var importedPage = writer.GetImportedPage(reader, pageNumber);
                        var pageRotation = reader.GetPageRotation(pageNumber);
                        var pageWidth = rect.Width;
                        var pageHeight = rect.Height;
                        switch (pageRotation)
                        {
                            case 0:
                                writer.DirectContent.AddTemplate(importedPage, 1f, 0, 0, 1f, 0, 0);
                                break;

                            case 90:
                                writer.DirectContent.AddTemplate(importedPage, 0, -1f, 1f, 0, 0, pageHeight);
                                break;

                            case 180:
                                writer.DirectContent.AddTemplate(importedPage, -1f, 0, 0, -1f, pageWidth, pageHeight);
                                break;

                            case 270:
                                writer.DirectContent.AddTemplate(importedPage, 0, 1f, -1f, 0, pageWidth, 0);
                                break;

                            default:
                                throw new InvalidOperationException(string.Format("Unexpected page rotation: [{0}].", pageRotation));
                        }                        
                    }

                    reader.Close();
                    document.Close();
                    writer.Close();
                    memoryBufferOut.Seek(0, SeekOrigin.Begin);
                    await memoryBufferOut.CopyToAsync(contentsOut);

                }
                catch (Exception)
                {
                    // well this is awkward. We need to send the file anyways.
                    memoryBufferIn.Seek(0, SeekOrigin.Begin);
                    await memoryBufferIn.CopyToAsync(contentsOut);
                }
                finally
                {
                    // this janky iText port doesn't support IDisposable
                    try { document.Close(); } catch (Exception) { }
                    try { writer.Close(); } catch (Exception) { }
                    try { reader.Close(); } catch (Exception) { }
                }
            }
        }

        public class EventHelper : PdfPageEventHelper
        {
            // The text we are going to write into the footer
            public string FooterText { get; set; }

            public EventHelper(string text)
            {
                FooterText = text;
            }

            // This is the contentbyte object of the writer
            PdfContentByte cb;

            // this is the BaseFont we are going to use for the header / footer
            BaseFont bf = null;

            public override void OnOpenDocument(PdfWriter writer, Document document)
            {
                try
                {
                    // setup and cache the font and content writer
                    bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                    cb = writer.DirectContent;
                }
                catch (DocumentException) { } // swallow exceptions if we can't watermark for some reason
                catch (System.IO.IOException) { }
            }

            // at the end of each page, this will fire
            public override void OnEndPage(iTextSharp.text.pdf.PdfWriter writer, iTextSharp.text.Document document)
            {
                base.OnEndPage(writer, document);

                // start text path
                cb.BeginText();

                var fontSize = 12;
                var leftMargin = 10;

                cb.SetFontAndSize(bf, fontSize);

                // measure the text's width and height
                var width = bf.GetWidthPoint(FooterText, fontSize);
                var height = bf.GetAscentPoint(FooterText, fontSize);

                // setup translation for text drawing
                cb.SetTextMatrix(leftMargin, document.PageSize.GetBottom(height));

                // draw the text
                cb.ShowText(FooterText);

                // done with text path
                cb.EndText();
            }
        }
    }
}
