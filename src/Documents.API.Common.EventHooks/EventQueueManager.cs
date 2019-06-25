namespace Documents.API.Common.EventHooks
{
    using Documents.API.Client;
    using Documents.API.Common.Events;
    using Documents.API.Common.Models;
    using Documents.Queues.Messages;
    using System;
    using System.Threading.Tasks;

    public class EventQueueManager : EventQueueMapBase
    {
        public async override Task<bool> HandleEventAsync(
            EventBase incomingEvent,
            Func<string, object, Task> enqueue,
            Connection api
        )
        {
            if (incomingEvent is FileContentsUploadCompleteEvent)
            {
                var uploadEvent = incomingEvent as FileContentsUploadCompleteEvent;

                var file = await api.File.GetAsync(uploadEvent.FileIdentifier);

                if (file == null)
                    Console.WriteLine($"Bad FileKey in UploadEvent: {uploadEvent.FileIdentifier}");
                else
                {
                    var actions = FileHandling.GetFileActions(file);

                    if (actions.Thumbnails)
                    {
                        var folder = await api.Folder.GetAsync(uploadEvent.FileIdentifier);
                        await CreateImageGenMessages(uploadEvent, folder, enqueue);
                    }

                    if (actions.ConvertToPDF)
                        await CreateToPDFMessage(uploadEvent, enqueue);

                    if (actions.EXIF)
                        await CreateExifMessage(uploadEvent, enqueue);

                    if (actions.TextExtract)
                        await CreateTextExtractMessage(uploadEvent, enqueue);

                    if (actions.PDFOCR)
                        await CreatePDFOCRMessage(uploadEvent, enqueue);

                    if (actions.Transcode)
                        await CreateTranscodeMessages(uploadEvent, enqueue);

                    if (actions.TranscodeAudio)
                        await CreateTranscodeMessages(uploadEvent, enqueue, audio: true);

                }
            }

            return false;
        }

        public Task CreateTextExtractMessage(FileContentsUploadCompleteEvent fileUploadEvent, Func<string, object, Task> enqueue)
            => enqueue("TextExtract", new TextExtractMessage(fileUploadEvent.FileIdentifier));

        public Task CreateVoicebaseMessage(FileContentsUploadCompleteEvent fileUploadEvent, Func<string, object, Task> enqueue)
            => enqueue("Voicebase", new FileBasedMessage(fileUploadEvent.FileIdentifier));

        public async Task CreateTranscodeMessages(FileContentsUploadCompleteEvent fileUploadEvent, Func<string, object, Task> enqueue, bool audio = false)
        {
            await enqueue("Transcode", new TranscodeMessage(fileUploadEvent.FileIdentifier)
            {
                TranscodeConfiguration = audio
                    ? "AudioTranscode"
                    : "VideoTranscode"
            });
        }
            

        public Task CreateArchiveMessages(FileContentsUploadCompleteEvent fileUploadEvent, Func<string, object, Task> enqueue)
            => enqueue("Archive", new ArchiveMessage(fileUploadEvent.FileIdentifier));

        public Task CreateToPDFMessage(FileContentsUploadCompleteEvent fileUploadEvent, Func<string, object, Task> enqueue)
            => enqueue("ToPDF", new ConvertToPDFMessage(fileUploadEvent.FileIdentifier));

        public Task CreatePDFOCRMessage(FileContentsUploadCompleteEvent fileUploadEvent, Func<string, object, Task> enqueue)
            => enqueue("PDFOCR", new FileBasedMessage(fileUploadEvent.FileIdentifier));

        public Task CreateExifMessage(FileContentsUploadCompleteEvent fileUploadEvent, Func<string, object, Task> enqueue)
            => enqueue("ExifTool", new FileBasedMessage(fileUploadEvent.FileIdentifier));

        // Here we're going to fan out our file upload event to any image generation messages that need to happen
        public Task CreateImageGenMessages(FileContentsUploadCompleteEvent fileUploadEvent, FolderModel folder, Func<string, object, Task> enqueue)
            => enqueue("ImageGen", new ImageGenMessage { Identifier = fileUploadEvent.FileIdentifier });
    }
}
