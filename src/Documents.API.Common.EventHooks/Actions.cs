namespace Documents.API.Common.EventHooks
{
    public class Actions
    {
        public bool ConvertToPDF {get; set;} = false;
        public bool TextExtract {get; set;} = false;
        public bool PDFOCR {get; set;} = false;
        public bool Thumbnails {get; set;} = false;
        public bool Transcode {get; set;} = false;
        public bool TranscodeAudio {get; set;} = false;
        public bool EXIF {get; set;} = false;
    }
}
