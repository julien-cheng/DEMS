namespace Documents.API.Common.EventHooks
{
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.API.Common.Models.MetadataModels;

    public static class FileHandling
    {
        public static Actions GetFileActions(FileModel file)
        {
            var actions = new Actions();

            if (file != null)
            {
                string extension = file.Extension;

                if (!file.Read<bool>(MetadataKeyConstants.HIDDEN)
                    && file.Read<FileIdentifier>(MetadataKeyConstants.CHILDOF) == null
                )
                {
                    switch (extension)
                    {
                        case "txt":
                        case "vtt":
                            actions.TextExtract = true;
                            break;

                        case "doc":
                        case "docx":
                        case "rtf":
                        case "vsd":
                        case "vsdx":
                        case "xls":
                        case "xlsx":
                        case "csv":
                        // case "tif": // removed because unoconv is only handling single page tiffs
                        case "htm":
                        case "html":
                            actions.TextExtract = true;
                            actions.ConvertToPDF = true;
                            break;

                        case "pdf":
                            actions.TextExtract = true;
                            //pdfOCR = true; happens after textExtract IF there is no text found
                            break;

                        case "png":
                        case "jpg":
                        case "jpeg":
                        case "gif":
                        case "bmp":
                            actions.Thumbnails = true;
                            actions.EXIF = true;
                            break;

                        case "zip":
                            //await CreateArchiveMessages(uploadEvent, file, enqueue);
                            break;

                        case "mp3":
                            actions.EXIF = true;
                            break;

                        case "wav":
                        case "mp2":
                        case "aac":
                        case "m4a":
                        case "caf":
                        case "wma":
                        case "ogg":
                        case "flac":
                        case "alac":
                            actions.TranscodeAudio = true;
                            actions.EXIF = true;
                            break;

                        case "wmv":
                        case "asx":
                        case "asf": // ms videos
                        case "vob": // dvd
                        case "mov":
                        case "avi":
                        case "mpg":
                        case "mpeg":
                        case "mp4":
                        case "flv":
                        case "webm":
                        case "mts":
                            actions.EXIF = true;
                            actions.Transcode = true;
                            break;
                    }

                }
                else // childof or hidden
                {
                    switch (extension)
                    {
                        case "vtt":
                            actions.TextExtract = true;
                            break;
                    }
                }
            }

            return actions;
        }
    }
}
