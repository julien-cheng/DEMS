namespace Documents.Clients.Admin.Controllers
{
    using CsvHelper;
    using Documents.API.Common.Models;
    using Documents.Clients.Admin.Models;
    using Microsoft.AspNetCore.Mvc;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    public class AccountingController : Controller
    {
        private readonly APIConnection API;

        public AccountingController(APIConnection api)
        {
            API = api;
        }

        public async Task<IActionResult> Index()
        {

            return View(await GetOrganizations());
        }

        public async Task<List<OrganizationModel>> GetOrganizations()
        {
            var allOrgs = await API.Organization.GetAllAsync();

            return allOrgs.Rows.ToList();
        }

        public async Task<IActionResult> Transcription(string organizationKey, string startDate, string endDate, string reportAction)
        {
            var sample = DateTime.UtcNow;
            var rangeStart = startDate == null
                ? new DateTime(sample.Year, sample.Month, 1).AddMonths(-1)
                : DateTime.Parse(startDate);
            var rangeEnd = endDate == null
                ? rangeStart.AddMonths(1)
                : DateTime.Parse(endDate);

            var report = await GetReport(rangeStart, rangeEnd, organizationKey);

            if (reportAction == "csv")
            {
                using (var sw = new StringWriter())
                {
                    var csv = new CsvWriter(sw);
                    csv.WriteHeader<ReportRow>();
                    csv.NextRecord();
                    csv.WriteRecords(report);

                    csv.Flush();
                    sw.Flush();

                    var csvReport = sw.ToString();
                    var msReport = new MemoryStream();

                    try
                    {
                        var reportBytes = System.Text.Encoding.UTF8.GetBytes(sw.ToString());
                        msReport.Write(reportBytes, 0, reportBytes.Length);

                        msReport.Seek(0, SeekOrigin.Begin);
                    }
                    catch (Exception)
                    {
                        msReport.Dispose();
                        throw;
                    }


                    return File(msReport, "text/csv", "report.csv");
                }
            }
            else
                return View(new ReportModel
                {
                    OrganizationKey = organizationKey,
                    Start = rangeStart,
                    End = rangeEnd,
                    Report = report
                });

        }

        public async Task<List<ReportRow>> GetReport(DateTime rangeStart, DateTime rangeEnd, string organizationKey)
        {

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

            var orgLookup = new Dictionary<string, OrganizationModel>();

            foreach (var row in logs.Rows.Where(r => organizationKey == null || r.OrganizationIdentifier.OrganizationKey == organizationKey))
            {
                try
                {
                    var detail = row.Details != null
                        ? JsonConvert.DeserializeObject<TranscriptionDetail>(row.Details)
                        : null;


                    if (!orgLookup.ContainsKey(row.FileIdentifier.OrganizationKey))
                    {
                        orgLookup.Add(
                            row.FileIdentifier.OrganizationKey,
                            await API.Organization.GetAsync(row.FileIdentifier)
                        );
                    }

                    if (detail != null)
                    {
                        var file = await API.File.GetAsync(row.FileIdentifier);
                        report.Add(new ReportRow
                        {
                            OrganizationKey = file.Identifier.OrganizationKey,
                            OrganizationName = orgLookup[file.Identifier.OrganizationKey]?.Name,
                            Folder = file.Identifier.FolderKey,
                            File = file.Identifier.FileKey,
                            Name = file.Name,
                            Date = row.Generated,
                            User = row.InitiatorUserIdentifier.UserKey,
                            Duration = detail.Length,
                            Status = detail.Status,
                            MediaID = detail.MediaID
                        });
                    }
                }
                catch (Exception) { }
            }

            return report;
        }

        private class TranscriptionDetail
        {
            public string MediaID { get; set; }
            public string Status { get; set; }
            public int Length { get; set; }
        }
    }
}
