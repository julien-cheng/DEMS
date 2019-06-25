namespace Documents.Queues.Tasks.ExifTool
{
    using Documents.API.Client;
    using Documents.API.Common.EventHooks;
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.API.Common.Models.MetadataModels;
    using Documents.Queues.Messages;
    using Documents.Queues.Tasks.Configuration;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    class ExifToolTask :
        QueuedApplication<ExifToolTask.ExifToolConfiguration, FileBasedMessage>
    {
        protected override string ConfigurationSectionName => "DocumentsQueuesTasksExifTool";
        protected override string QueueName => "ExifTool";

        protected override async Task Process()
        {
            var fileModel = await GetFileAsync();

            var actions = FileHandling.GetFileActions(fileModel);

            if (actions.EXIF)
            {
                await DownloadAsync();

                await ExifToolAsync(API, fileModel, Configuration.Executable, this.LocalFilePath, Configuration.Arguments);
            }
        }

        private async Task ExifToolAsync
        (
            Connection api,
            FileModel fileModel,
            string executable,
            string input,
            string arguments
        )
        {
            var stdout = ExifToolExecute(executable, input, arguments);
            stdout = Regex.Replace(stdout, @"\r\n|\n\r|\n|\r", "\r\n");

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
                            Name = $"EXIF-{fileModel.NameWithoutExtension()}.txt",
                            FilePrivileges = fileModel.FilePrivileges
                        };

                        // if the file we were extracting from was a child itself, attach our
                        // results to its parent
                        var childOfFileIdentifier = fileModel.Read<FileIdentifier>("_childof") ?? fileModel.Identifier;

                        textModel.InitializeEmptyMetadata();
                        textModel.Write(MetadataKeyConstants.CHILDOF, childOfFileIdentifier);
                        textModel.Write(MetadataKeyConstants.HIDDEN, true);

                        var reportFields = ParseReport(stdout);
                        await api.ConcurrencyRetryBlock(async () =>
                        {
                            var original = await api.File.GetAsync(fileModel.Identifier);
                            ExtractAttributes(original, reportFields);
                            await api.File.PutAsync(original);
                        });

                        textModel = await api.File.UploadAsync(textModel, ms);

                        await TagAlternativeView(childOfFileIdentifier, textModel.Identifier, new Documents.API.Common.Models.MetadataModels.AlternativeView
                        {
                            FileIdentifier = textModel.Identifier,
                            MimeType = textModel.MimeType,
                            Name = "EXIF"
                        });
                    }
                }
            }
        }

        private void ExtractAttributes(FileModel file, Dictionary<string, string> values)
        {
            try
            {
                WriteAttribute(file, "Create Date", "created", values, (f) =>
                {
                    var parts = f.Split(':', ' ');
                    if (parts.Length > 4)
                    {
                        int year = int.Parse(parts[0]);
                        int month = int.Parse(parts[1]);
                        int day = int.Parse(parts[2]);
                        int hour = int.Parse(parts[3]);
                        int minute = int.Parse(parts[4]);
                        int second = int.Parse(parts[5]);

                        return new DateTime(year, month, day, hour, minute, second).ToString("MM/dd/yyyy H:mm:ss");

                    }
                    else
                        return f;
                });
            }
            catch (Exception) { }

            WriteAttribute(file, "Make", "make", values);
            WriteAttribute(file, "Camera Model Name", "model", values);
            WriteAttribute(file, "GPS Position", "gps", values, f => f.Replace(" deg", "°"));

            WriteAttribute(file, "Date/Time Original", "datetimeoriginal", values);
            WriteAttribute(file, "Lens ID", "lensid", values);
            WriteAttribute(file, "Software", "software", values);
            WriteAttribute(file, "File Size", "filesize", values);
            WriteAttribute(file, "ISO Setting", "isosetting", values);
            WriteAttribute(file, "Shutter Speed", "shutterspeed", values);
            WriteAttribute(file, "F Number", "fnumber", values);
            WriteAttribute(file, "Flash", "flash", values);
            WriteAttribute(file, "Megapixels", "megapixels", values);
            WriteAttribute(file, "Image Unique ID", "imageuniqueid", values);
            WriteAttribute(file, "Codec", "codec", values);
            WriteAttribute(file, "Frame Rate", "framerate", values);
            WriteAttribute(file, "Frame Count", "framecount", values);
            WriteAttribute(file, "Duration", "duration", values, (f) =>
            {
                var parts = f.Split(':');
                if (parts.Length == 3)
                {
                    int Hour = 0;
                    int Minute = 0;
                    int Seconds = 0;
                    int.TryParse(parts[0], out Hour);
                    int.TryParse(parts[1], out Minute);
                    int.TryParse(parts[2], out Seconds);

                    return ((Hour * 60 * 60) + (Minute * 60) + Seconds).ToString();
                }

                if (f.ToLower().Trim().EndsWith("s"))
                {
                    return f.Replace("s", "");
                }

                return f;
            });

            if (GetField("X Resolution", values) != null)
            {
                var x = GetField("X Resolution", values);
                var y = GetField("Y Resolution", values);
                file.Write($"attribute.resolution", $"{x} x {y}");
            }

            // resolution


        }

        private string GetField(string key, Dictionary<string, string> values)
        {
            if (!values.ContainsKey(key.ToLower()))
                return null;

            var value = values[key.ToLower()];

            if (!string.IsNullOrWhiteSpace(value))
                return value;
            else
                return null;
        }

        private void WriteAttribute(FileModel file, string key, string attributename, Dictionary<string, string> values, Func<string, string> format = null)
        {
            var value = GetField(key, values);

            if (value != null)
            {
                if (format != null)
                    value = format(value);

                file.Write($"attribute.{attributename}", value);
            }

        }

        private Dictionary<string, string> ParseReport(string report)
        {
            var lines = report.Split("\r\n");
            var fields = new Dictionary<string, string>();

            foreach (var line in lines)
            {
                var parts = Regex.Split(line, @"\s*\:\s+");
                if (parts.Length > 1)
                {
                    var key = parts[0]?.Trim()?.ToLower();
                    var value = parts[1]?.Trim();

                    if (!fields.ContainsKey(key))
                        fields.Add(key, value);
                }
            }

            return fields;
        }

        private string ExifToolExecute(
            string executable,
            string inputFile,
            string arguments
        )
        {
            UseVar(ref arguments, "input", inputFile);

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
                stdout.AppendLine(args.Data);

            p.Start();
            p.BeginOutputReadLine();
            p.WaitForExit();

            return stdout.ToString();
        }

        private static string UseVar(ref string original, string variable, string value)
        {
            return original = original.Replace($":{variable}:", value);
        }

        public class ExifToolConfiguration : TaskConfiguration
        {
            public string Executable { get; set; } = "exiftool";
            public string Arguments { get; set; } = ":input:";

            public override string SectionName => "DocumentsQueuesTasksExifTool";
        }

    }
}
