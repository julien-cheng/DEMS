namespace Documents.Queues.Tasks.Transcode.FFMPEG
{
    using Documents.API.Client;
    using Documents.API.Common.Exceptions;
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.API.Common.Models.MetadataModels;
    using Documents.Queues.Messages;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;

    class TranscodeTask :
        QueuedApplication<FFMPEGConfiguration, TranscodeMessage>
    {
        protected override string ConfigurationSectionName => "DocumentsQueuesTasksTranscodeFFMPEG";
        protected override string QueueName => "Transcode";

        protected override async Task Process()
        {
            try
            {
                var fileModel = await GetFileAsync();
                await DownloadAsync();

                var task = Configuration.Tasks[CurrentMessage.TranscodeConfiguration];
                foreach (var step in task)
                    await ConvertToFileAsync(
                        api: API,
                        tmpInput: this.LocalFilePath,
                        outputExtension: step.Extension,
                        newFileName: step.NewName,
                        arguments: step.Arguments,
                        fileModel: fileModel,
                        contentType: step.ContentType
                    );
            }
            catch (DocumentsNotFoundException)
            {
                Logger.LogWarning("Object not found while trying to Transcode a a PDF. Object likely deleted before task started. Moving on.");
            }
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

        private async Task<bool> ConvertToFileAsync(
            Connection api,
            string tmpInput,
            string outputExtension,
            string newFileName,
            FileModel fileModel,
            string arguments,
            string contentType
        )
        {
            string tmpOutput = null;
            if (!string.IsNullOrWhiteSpace(outputExtension))
                tmpOutput = tmpInput + outputExtension;

            if (!string.IsNullOrWhiteSpace(fileModel.Name)
                && !string.IsNullOrWhiteSpace(newFileName))
                UseVar(ref newFileName, "original", fileModel.Name);

            if (FFMPEGExecute(tmpInput, tmpOutput, arguments))
            {
                if (!string.IsNullOrWhiteSpace(newFileName))
                {
                    Console.WriteLine($"Uploading {newFileName}");

                    var newFile = await api.File.FileModelFromLocalFileAsync(
                        tmpOutput, new FileIdentifier(fileModel.Identifier as FolderIdentifier, null));

                    newFile.Name = newFileName;
                    newFile.MimeType = contentType;
                    newFile.Write(MetadataKeyConstants.HIDDEN, true);
                    newFile.Write(MetadataKeyConstants.CHILDOF, fileModel.Identifier);

                    newFile = await api.File.UploadLocalFileAsync(tmpOutput, newFile);

                    var alternativeView = new AlternativeView()
                    {
                        FileIdentifier = newFile.Identifier,
                        MimeType = contentType,
                        Name = "transcode"
                    };

                    await base.TagAlternativeView(fileModel.Identifier, newFile.Identifier, alternativeView);
                }
                return true;
            }

            return false;
        }

        private static string UseVar(ref string original, string variable, string value)
        {
            return original = original.Replace($":{variable}:", value);
        }

    }
}
