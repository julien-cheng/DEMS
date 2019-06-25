namespace Documents.Queues.Tasks.LogReader
{
    using Documents.API.Client;
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.Queues.Messages;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class UploadNotify
    {
        public static async Task ScanForUploadBatches(Connection API, LogReaderTask.LogReaderConfiguration configuration)
        {
            var rangeStart = DateTime.UtcNow.AddMinutes(configuration.UploadLookBackMinutes * -1);
            var rangeEnd = DateTime.UtcNow;

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
                            Value = "FileContentsUploadCompleteEvent"
                        },
                        new MetadataMatchModel
                        {
                            Name = "Generated",
                            Operator = ">=",
                            Value = rangeStart.ToString()
                        },
                        new MetadataMatchModel
                        {
                            Name = "Generated",
                            Operator = "<",
                            Value = rangeEnd.ToString()
                        }
                    }
                }
            });

            var byFolder = new Dictionary<FolderIdentifier, List<AuditLogEntryModel>>();

            foreach (var row in logs.Rows)
            {
                var folderIdentifier = row.FolderIdentifier;
                if (!byFolder.ContainsKey(folderIdentifier))
                    byFolder.Add(folderIdentifier, new List<AuditLogEntryModel>());

                byFolder[folderIdentifier].Add(row);
            }

            foreach (var folderIdentifier in byFolder.Keys)
            {
                var uploads = byFolder[folderIdentifier];
                
                var lastDate = uploads.Max(f => f.Generated);

                if (DateTime.UtcNow.Subtract(lastDate).TotalMilliseconds > configuration.UploadNotifyAfterMS)
                {
                    var fileList = new List<FileModel>();

                    foreach (var row in uploads)
                    {
                        await API.ConcurrencyRetryBlock(async () =>
                        {
                            var file = await API.File.GetAsync(row.FileIdentifier);
                            if (file != null && !file.Read<bool>("_hidden", defaultValue: false))
                            {
                                if (file.Read<DateTime?>("_uploadnotified") == null)
                                {
                                    file.Write("_uploadnotified", DateTime.UtcNow);
                                    await API.File.PutAsync(file);
                                    fileList.Add(file);
                                }
                            }
                        });
                    }
                    if (fileList.Any())
                    {
                        var folderModel = await API.Folder.GetAsync(folderIdentifier);

                        // let's see if an email exists for the ada in the metadata
                        var adaEmail = folderModel.Read<string>("attribute.ada.email");
                        if (adaEmail != null)
                        {
                            var userIdentifier = new UserIdentifier(folderModel.Identifier as OrganizationIdentifier, adaEmail);
                            var user = await API.User.GetAsync(userIdentifier);

                            // does that ada have a user account
                            if (user != null && user.UserAccessIdentifiers.Any(i => i == "g:SendNotify"))
                            {

                                var organization = await API.Organization.GetAsync(folderModel.Identifier);
                                var linkURL = configuration.UploadCaseLinkURL;

                                string defendantID = folderIdentifier.FolderKey.Replace("Defendant:", "");
                                linkURL = string.Format(linkURL, defendantID);

                                await API.Queue.EnqueueAsync("notify", new NotifyMessage
                                {
                                    TemplateName = "upload",
                                    Model = new
                                    {
                                        DefendantFirstName = folderModel.Read<string>("attribute.firstname"),
                                        DefendantLastName = folderModel.Read<string>("attribute.lastname"),
                                        CaseNumber = folderModel.Read<string>("attribute.casenumber"),
                                        URL = linkURL,
                                        DefendantID = defendantID,
                                        Files =
                                        "<table>"
                                        + " <tr>"
                                        + "  <th>Path</th>"
                                        + "  <th>Name</th>"
                                        + "  <th>Size</th>"
                                        + " </tr>"
                                        + string.Join("", fileList.Select(f =>
                                            "<tr>"
                                            + $"<td>{f.Read<string>("_path")}</td>"
                                            + $"<td>{f.Name}</td>"
                                            + $"<td>{f.LengthForHumans}</td>"
                                            + "</tr>"
                                        ))
                                        + "</table>"


                                        // some awful interaction between newtonsoft and stubble
                                        // is preventing sending this neatly and dealing with
                                        // it in the template. 
                                        /*fileList.OrderBy(f => f.Name).Select(f => new
                                        {
                                            Path = f.Read<string>("_path"),
                                            f.Name
                                        })*/
                                    },
                                    RecipientIdentifier = userIdentifier
                                });
                            }
                        }
                    }
                }
            }
        }
    }
}
