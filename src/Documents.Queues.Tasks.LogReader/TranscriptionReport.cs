/*namespace Documents.Queues.Tasks.LogReader
{
    using CsvHelper;
    using Documents.API.Client;
    using Documents.API.Common.Models;
    using Documents.Queues.Messages;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    public class TranscriptionReport
    {
        public static async Task CreateReport(Connection API, LogReaderTask.LogReaderConfiguration configuration)
        {
            var sample = DateTime.UtcNow;
            var rangeStart = new DateTime(sample.Year, sample.Month, 1).AddMonths(-1);
            var rangeEnd = rangeStart.AddMonths(1);

            var report = new List<ReportRow>();

            var logs = await API.Log.LoadAsync(new[]
            {
                new PopulationDirective
                {
                    MetadataFilter = new List<MetadataMatchModel>
                    {
                        new MetadataMatchModel
                        {
                            Name = "ActionType",
                            Operator = "==",
                            Value = "Transcription"
                        },
                        new MetadataMatchModel
                        {
                            Name = "Generated",
                            Operator = ">=",
                            Value = rangeStart.ToShortDateString()
                        },
                        new MetadataMatchModel
                        {
                            Name = "Generated",
                            Operator = "<",
                            Value = rangeEnd.ToShortDateString()
                        }
                    }
                }
            });
                
            foreach (var row in logs.Rows.Where(r => r.FileIdentifier != null))
            {
                try
                {
                    var detail = row.Details != null
                        ? JsonConvert.DeserializeObject<TranscriptionDetail>(row.Details)
                        : new TranscriptionDetail();

                    var file = await API.File.GetAsync(row.FileIdentifier);
                    var user = await API.User.GetAsync(row.InitiatorUserIdentifier);

                    report.Add(new ReportRow
                    {
                        Organization = file.Identifier.OrganizationKey,
                        Folder = file.Identifier.FolderKey,
                        File = file.Identifier.FileKey,
                        Name = file.Name,
                        Date = row.Generated,
                        User = user.EmailAddress ?? user.Identifier.UserKey,
                        Duration = detail.Length,
                        Status = detail.Status,
                        MediaID = detail.MediaID
                    });
                }
                catch (Exception e)
                {
                    
                }
            }

            using (var sw = new StringWriter())
            {
                var csv = new CsvWriter(sw);
                csv.WriteHeader<ReportRow>();
                csv.NextRecord();
                csv.WriteRecords(report);

                csv.Flush();
                sw.Flush();

                var csvReport = sw.ToString();
                using (var msReport = new MemoryStream())
                {
                    var reportBytes = System.Text.Encoding.UTF8.GetBytes(sw.ToString());
                    msReport.Write(reportBytes, 0, reportBytes.Length);

                    msReport.Seek(0, SeekOrigin.Begin);

                    var folder = await API.Folder.GetAsync(configuration.OutputFolder);
                    if (folder == null)
                        folder = await API.Folder.PutAsync(new FolderModel(configuration.OutputFolder));

                    var file = await API.File.UploadAsync(
                        new FileModel(new FileIdentifier(folder.Identifier))
                        {
                            Created = DateTime.UtcNow,
                            Modified = DateTime.UtcNow,
                            MimeType = "text/csv",
                            Name = $"TranscriptReport.{DateTime.UtcNow.ToString("yyyy.MM")}.csv",
                            Length = reportBytes.Length
                        },
                        msReport);

                    foreach (var recipient in configuration.Recipients)
                    {
                        await API.Queue.EnqueueAsync("Notify", new NotifyMessage
                        {
                            RecipientIdentifier = recipient,
                            TemplateName = configuration.NotifyTemplateName,
                            Attachments = new List<FileIdentifier> { file.Identifier },
                            Model = file
                        });
                    }
                }
            }
        }

        private class TranscriptionDetail
        {
            public string MediaID { get; set; }
            public string Status { get; set; }
            public int Length { get; set; }
        }

        public class ReportRow
        {
            public DateTime Date { get; set; }
            public string User { get; set; }
            public string Organization { get; set; }
            public int Duration { get; set; }
            public string Status { get; set; }
            public string MediaID { get; set; }
            public string Folder { get; set; }
            public string File { get; set; }
            public string Name { get; set; }
        }
    }
}
*/