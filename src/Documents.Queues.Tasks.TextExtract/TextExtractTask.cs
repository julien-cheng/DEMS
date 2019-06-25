namespace Documents.Queues.Tasks.TextExtract
{
    using Documents.API.Client;
    using Documents.API.Common.Exceptions;
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.API.Common.Models.MetadataModels;
    using Documents.Queues.Messages;
    using Documents.Queues.Tasks.TextExtract.Configuration;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;

    class TextExtractTask :
        QueuedApplication<TextExtractConfiguration, TextExtractMessage>
    {
        protected override string ConfigurationSectionName => "DocumentsQueuesTasksTextExtract";
        protected override string QueueName => "TextExtract";

        protected override async Task Process()
        {
            try
            {
                var fileModel = await GetFileAsync();
                await this.DownloadAsync();

                foreach (var step in Configuration.Steps)
                {
                    if (fileModel != null)
                        await ExtractAsync(
                            api: API,
                            fileModel: fileModel,
                            executable: step.Executable,
                            arguments: step.Arguments,
                            input: this.LocalFilePath,
                            tag: step.Tag,
                            extension: step.Extension,
                            contentType: step.ContentType
                        );
                    else
                        throw new Exception("File does not exist");
                }
            }
            catch (DocumentsNotFoundException)
            {
                // if it doesn't exist, there's not text to extract
                Logger.LogWarning($"Cannot extract text from non-existant: {CurrentMessage.Identifier}");
                return;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private string ExtractExecute(
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
                    RedirectStandardOutput = true,
                    StandardOutputEncoding = Encoding.UTF8
                }
            };

            var stdout = new StringBuilder();
            p.OutputDataReceived += (sender, args) =>
                stdout.AppendLine(args.Data);

            p.Start();
            p.BeginOutputReadLine();
            p.WaitForExit();

            return stdout.ToString();
        }

        private async Task<string> ExtractAsync(
            Connection api,
            FileModel fileModel,
            string executable,
            string input,
            string arguments,
            string tag,
            string extension,
            string contentType
        )
        {
            var output = $"{input}.{extension}";
            var stdout = ExtractExecute(executable, input, output, arguments);

            if (!string.IsNullOrWhiteSpace(stdout))
            {
                using (var ms = new MemoryStream())
                using (var sw = new StreamWriter(ms))
                {
                    sw.Write(stdout);
                    await sw.FlushAsync();

                    if (ms.Length > 0)
                    {
                        ms.Seek(0, SeekOrigin.Begin);

                        var textModel = new FileModel
                        {
                            Identifier = new FileIdentifier(fileModel.Identifier as FolderIdentifier, Guid.NewGuid().ToString()),
                            Created = DateTime.UtcNow,
                            Modified = DateTime.UtcNow,
                            Length = ms.Length,
                            MimeType = "text/plain",
                            Name = "extracted text",
                            FilePrivileges = fileModel.FilePrivileges
                        };

                        // if the file we were extracting from was a child itself, attach our
                        // results to its parent
                        var childOfFileIdentifier = fileModel.Read<FileIdentifier>("_childof") ?? fileModel.Identifier;

                        textModel.InitializeEmptyMetadata();

                        textModel.Write(MetadataKeyConstants.CHILDOF, childOfFileIdentifier);
                        textModel.Write(MetadataKeyConstants.HIDDEN, true);

                        textModel = await api.File.UploadAsync(textModel, ms);

                        await TagAlternativeView(childOfFileIdentifier, textModel.Identifier, new Documents.API.Common.Models.MetadataModels.AlternativeView
                        {
                            FileIdentifier = textModel.Identifier,
                            MimeType = textModel.MimeType,
                            Name = "text"
                        });

                        /*await api.Queue.EnqueueAsync("Index", new IndexMessage
                        {
                            Identifier = childOfFileIdentifier,
                            Action = IndexMessage.IndexActions.IndexFile
                        });*/
                    }
                }

                Console.WriteLine("Done");
            }
            else
            {
                if (Configuration.OCRPDFsIfNoText && fileModel.Extension == "pdf")
                {
                    // ensure this is a user uploaded file, not already a searchable output or other artifact.
                    var childOf = fileModel.Read<FileIdentifier>(MetadataKeyConstants.CHILDOF);
                    if (childOf == null)
                    {
                        // there was no text found by the extractor.. if it's a PDF, let's OCR it.
                        await api.Queue.EnqueueAsync("PDFOCR", new FileBasedMessage(fileModel.Identifier));
                    }
                }
            }

            return stdout;
        }

        private string UseVar(ref string original, string variable, string value)
        {
            return original = original.Replace($":{variable}:", value);
        }
    }
}
