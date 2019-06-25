namespace Documents.Queues.Tasks.Transcode.FFMPEG
{
    using Documents.API.Common.Exceptions;
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.Queues.Messages;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    class VideoToolsTask :
        QueuedApplication<FFMPEGConfiguration, VideoToolsMessage>
    {
        protected override string ConfigurationSectionName => "DocumentsQueuesTasksTranscodeFFMPEG";
        protected override string QueueName => "VideoTools";

        protected override async Task Process()
        {
            try
            {
                var fileModel = await GetFileAsync();
                await DownloadAsync("." + fileModel.Extension);


                string outputPath = null;
                string contentType = null;

                if (CurrentMessage.Clipping != null)
                {
                    outputPath = CreateTemporaryFile(".mp4");
                    ExportClip(CurrentMessage.Clipping, outputPath);
                    contentType = "video/mp4";
                }
                else if (CurrentMessage.Frame != null)
                {
                    outputPath = CreateTemporaryFile(".png");
                    ExportFrame(CurrentMessage.Frame, outputPath);
                    contentType = "image/png";
                }
                else if (CurrentMessage.Watermark != null)
                {
                    outputPath = CreateTemporaryFile(".mp4");
                    await ExportWatermarkedAsync(CurrentMessage.Watermark, outputPath);
                    contentType = "video/mp4";
                }
                else
                    throw new Exception("Unsupported VideoTools request");

                var fileIdentifier = new FileIdentifier(fileModel.Identifier, Guid.NewGuid().ToString());
                var outputFile = await API.File.FileModelFromLocalFileAsync(outputPath, fileIdentifier);

                outputFile.Name = CurrentMessage.OutputName;
                outputFile.MimeType = contentType;

                var path = CurrentMessage.Path;
                if (path == null)
                {
                    var inheritFrom = fileModel;

                    var parentIdentifier = inheritFrom.Read<FileIdentifier>("_childof");
                    if (parentIdentifier != null)
                        inheritFrom = await API.File.GetAsync(parentIdentifier);

                    path = inheritFrom?.Read<string>("_path");
                }
                outputFile.Write("_path", path);

                await API.File.UploadLocalFileAsync(outputPath, outputFile);
            }
            catch (DocumentsNotFoundException)
            {
                Logger.LogWarning("Object not found in Video Tools, moving on.");
            }
        }


        private void ExportClip(VideoToolsMessage.ClippingDetails clipping, string outputPath)
        {
            var mutes = clipping.MutedRanges?.Any() ?? false
                ? "-af \"" + string.Join(", ", clipping.MutedRanges.Select(r =>
                        $"volume=enable='between(t,{FFSeconds(r.StartTimeMS)},{FFSeconds(r.EndTimeMS)})':volume=0"
                    ))
                    + "\""
                : string.Empty;

            var arguments = $"-i :input: {mutes} -ss {FFSeconds(clipping.StartTimeMS)}"
                + $" -to {FFSeconds(clipping.EndTimeMS)} :output:";

            if (!FFMPEGExecute(LocalFilePath, outputPath, arguments))
                throw new Exception("ffmpeg exited non-zero");
        }

        private void ExportFrame(VideoToolsMessage.ExportFrameDetails frameDetails, string outputPath)
        {
            var arguments = $"-i :input: -ss {FFSeconds(frameDetails.StartTimeMS)} -vframes 1 :output:";

            if (!FFMPEGExecute(LocalFilePath, outputPath, arguments))
                throw new Exception("ffmpeg exited non-zero");
        }

        private async Task ExportWatermarkedAsync(VideoToolsMessage.WatermarkingDetails watermarking, string outputPath)
        {
            string watermarkFile = CreateTemporaryFile(".png");

            using (var watermarkStream = File.OpenWrite(watermarkFile))
                await API.File.DownloadAsync(watermarking.Watermark, watermarkStream);

            var arguments = $"-i :input: "
                + $"-i {watermarkFile} -filter_complex \"overlay=x=(main_w-overlay_w):y=(main_h-overlay_h)/(main_h-overlay_h)\" -c:v libx264 :output:";
            //overlay=x=(main_w-overlay_w):y=(main_h-overlay_h)/(main_h-overlay_h)
            if (!FFMPEGExecute(LocalFilePath, outputPath, arguments))
                throw new Exception("ffmpeg exited non-zero");
        }

        private string FFSeconds(int ms)
        {
            return ((decimal)ms / 1000).ToString("0.000");
        }

        private bool FFMPEGExecute(
            string inputFile,
            string outputFile,
            string arguments
        )
        {
            UseVar(ref arguments, "input", inputFile);
            UseVar(ref arguments, "output", outputFile);

            Console.WriteLine($"Executing: {Configuration.Executable} {arguments}");

            var startInfo = new ProcessStartInfo
            {
                Arguments = arguments,
                WorkingDirectory = Directory.GetCurrentDirectory(),
                FileName = Configuration.Executable,
                UseShellExecute = false
            };

            var process = System.Diagnostics.Process.Start(startInfo);
            process.WaitForExit();

            return process.ExitCode == 0;
        }

        private static string UseVar(ref string original, string variable, string value)
        {
            return original = original.Replace($":{variable}:", value);
        }

        // we don't want any reentrance detection
        protected override void ValidateFile(FileModel fileModel)
        {
            if (fileModel == null)
                throw new TaskValidationException("File does not exist");
        }
    }
}
