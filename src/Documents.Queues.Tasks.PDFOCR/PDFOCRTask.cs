namespace Documents.Queues.Tasks.PDFOCR
{
    using Documents.API.Client;
    using Documents.API.Common.Exceptions;
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.API.Common.Models.MetadataModels;
    using Documents.Queues.Messages;
    using Documents.Queues.Tasks.PDFOCR.Configuration;
    using iTextSharp.text.pdf;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;

    class PDFOCRTask : 
        QueuedApplication<PDFOCRConfiguration, FileBasedMessage>
    {
        protected override string ConfigurationSectionName => "DocumentsQueuesTasksPDFOCR";
        protected override string QueueName => "PDFOCR";
        protected int NumberOfPages = 0;
        
        protected override async Task Process()
        {
            try
            {
                NumberOfPages = 0;

                var fileModel = await GetFileAsync();
                await DownloadAsync(fileExtensionWithDot: ".pdf");

                var pdfReader = new PdfReader(LocalFilePath);
                NumberOfPages = pdfReader.NumberOfPages;

                await PDFOCRAsync(API, fileModel, Configuration.Executable, this.LocalFilePath, Configuration.Arguments);
            }
            catch (DocumentsNotFoundException)
            {
                Logger.LogWarning("Object not found while trying to OCR a PDF. Object likely deleted before task started. Moving on.");
            }
        }

        protected override object TaskCompletionDetails()
        {
            return new
            {
                QueueName,
                CurrentMessage,
                NumberOfPages,
                LastFileModelLogging?.Identifier.OrganizationKey,
                LastFileModelLogging?.Identifier.FolderKey,
                LastFileModelLogging?.Identifier.FileKey,
                LastFileModelLogging?.Length,
                LastFileModelLogging?.Name,
                LastFileModelLogging?.MimeType
            };
        }

        private string PDFOCRExecute(
            string executable,
            string inputFile,
            string outputFile,
            string arguments
        )
        {
            UseVar(ref arguments, "input", inputFile);
            UseVar(ref arguments, "output", outputFile);

            Console.WriteLine($"Executing: {executable} {arguments}");

            var p = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    Arguments = arguments,
                    WorkingDirectory = Directory.GetCurrentDirectory(),
                    FileName = executable,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                }
            };
            var stdout = new StringBuilder();
            p.OutputDataReceived += (sender, args) =>
                stdout.Append(args.Data);

            p.Start();
            p.BeginOutputReadLine();
            var watchdogTimerLimit = Configuration.MaximumSecondsPerPage * NumberOfPages * 1000;
            if (!p.WaitForExit(watchdogTimerLimit))
            {
                p.Kill();
                throw new Exception("Process did not complete in the configured amount of time");
            }
            
            return stdout.ToString();
        }

        private async Task<string> PDFOCRAsync
        (
            Connection api,
            FileModel fileModel,
            string executable,
            string input,
            string arguments
        )
        {
            var output = CreateTemporaryFile(".pdf");

            var stdout = PDFOCRExecute(executable, input, output, arguments);

            var newFileModel = await api.File.FileModelFromLocalFileAsync(
                output, 
                new FileIdentifier(fileModel.Identifier as FolderIdentifier, null)
            );

            newFileModel.Name = Path.GetFileNameWithoutExtension(fileModel.Name) + ".pdf";

            newFileModel.Write(MetadataKeyConstants.HIDDEN, true);
            newFileModel.Write(MetadataKeyConstants.CHILDOF, fileModel.Identifier);

            newFileModel = await api.File.UploadLocalFileAsync(
                output,
                newFileModel
            );

            await TagAlternativeView(fileModel.Identifier, newFileModel.Identifier, new AlternativeView
            {
                FileIdentifier = newFileModel.Identifier,
                MimeType = "application/pdf",
                Name = Configuration.OutputViewName
            });

            await api.Queue.EnqueueAsync("TextExtract", new TextExtractMessage(newFileModel.Identifier));

            return stdout;
        }

        private static string UseVar(ref string original, string variable, string value)
        {
            return original = original.Replace($":{variable}:", value);
        }
    }
}
