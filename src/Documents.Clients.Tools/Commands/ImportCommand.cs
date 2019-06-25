namespace Documents.Clients.Tools.Commands
{
    using Documents.API.Common.Events;
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.Clients.Tools.Models;
    using McMaster.Extensions.CommandLineUtils;
    using MoreLinq;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    [Subcommand("pcms", typeof(PCMS))]
    [Subcommand("drone", typeof(Drone))]
    class ImportCommand : CommandBase
    {
        class PCMS : CommandBase
        {
            [Required, Argument(0)]
            public string OrganizationIdentifier { get; }

            [Required, Argument(1)]
            public string FileName { get; }

            [Option("--match")]
            public string Match { get; } = null;

            [Option("--replace")]
            public string Replace { get; } = null;

            [Option("--task-type")]
            public ImportTask.TaskTypes TaskType { get; } = ImportTask.TaskTypes.Import;

            protected async override Task ExecuteAsync()
            {
                string json = null;

                Console.WriteLine("Reading file");
                try
                {
                    json = await System.IO.File.ReadAllTextAsync(FileName);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"Cannot read file {FileName}: {e}");
                    return;
                }

                Console.WriteLine("Parsing JSON");
                var tasks = JsonConvert.DeserializeObject<List<ImportTask>>(json);

                var total = tasks.Count;
                Console.WriteLine($"Found {total} tasks. Enqueueing.");
                var count = 0;

                var organizationIdentifier = GetOrganizationIdentifier(OrganizationIdentifier);

                foreach (var task in tasks)
                {
                    if (Match != null)
                        task.Filename = Regex.Replace(task.Filename, Match, Replace);


                    task.OrganizationKey = organizationIdentifier.OrganizationKey;
                    task.TaskType = TaskType;
                }

                foreach (var batch in tasks.Batch(1000))
                {
                    await API.Queue.EnqueueAsync("Import", batch);

                    count += batch.Count();

                    Console.WriteLine($"Enqueued {count}/{total} [{Math.Floor(1.0 * count / total * 100)}%]");
                }
            }
        }

        class Drone : CommandBase
        {
            [Option]
            public int Replicas { get; } = 1;

            [Option]
            public bool Drain { get; } = false;

            protected async override Task ExecuteAsync()
            {
                var drones = new List<Task>();

                for (var i = 0; i < Replicas; i++)
                    drones.Add(DroneEntry());

                await Task.WhenAll(drones);
            }

            private async Task DroneEntry()
            {
                var md5 = MD5.Create();

                while (true)
                {
                    using (var subscription = await API.Queue.SubscribeAsync("Import"))
                    {
                        FolderModel folderModel = null;

                        await subscription.ListenAsync(async (message) =>
                        {
                            try
                            {
                                var task = JsonConvert.DeserializeObject<ImportTask>(message.Message);

                                if (Drain)
                                {
                                    await subscription.Ack(message, true);
                                    return;
                                }

                                var folderIdentifier = new FolderIdentifier(task.OrganizationKey, task.FolderKey);

                                switch (task.TaskType)
                                {
                                    case ImportTask.TaskTypes.Import:
                                    case ImportTask.TaskTypes.SimulateUpload:
                                        if (System.IO.File.Exists(task.Filename))
                                        {

                                            if (folderIdentifier != folderModel?.Identifier)
                                            {
                                                folderModel = await API.Folder.GetAsync(folderIdentifier);
                                                if (folderModel == null)
                                                    folderModel = await API.Folder.PutAsync(new FolderModel(folderIdentifier));
                                            }


                                            var fileNameHash = md5.ComputeHash(
                                                Encoding.UTF8.GetBytes(
                                                    $"{folderIdentifier.OrganizationKey}/{folderIdentifier.FolderKey}/{task.Filename}"
                                                )
                                            );


                                            // convert to hexadecimal
                                            string fileKey = BitConverter.ToString(fileNameHash).Replace("-", "");
                                            
                                            var fileIdentifier = new FileIdentifier(folderIdentifier, fileKey);

                                            var fileModel = await API.File.GetAsync(fileIdentifier);
                                            if (fileModel != null)
                                            {
                                                if (task.TaskType == ImportTask.TaskTypes.SimulateUpload)
                                                {
                                                    await API.Queue.EnqueueAsync("EventRouter",
                                                        new FileContentsUploadCompleteEvent
                                                        {
                                                            FileIdentifier = fileIdentifier,
                                                            Generated = DateTime.UtcNow
                                                        });

                                                }
                                                Console.Error.WriteLine($"File already uploaded, skipping: {task.Filename}");
                                            }
                                            else
                                            {
                                                Console.WriteLine($"Start Uploading: {task.Filename}");

                                                fileModel = await API.File.FileModelFromLocalFileAsync(task.Filename, fileIdentifier);

                                                foreach (var key in task.Metadata.Keys)
                                                    fileModel.Write(key, task.Metadata[key]);

                                                await API.File.UploadLocalFileAsync(task.Filename, fileModel);
                                                Console.WriteLine($"Uploaded: {task.Filename}");
                                            }

                                            await subscription.Ack(message, true);
                                        }
                                        else
                                        {
                                            Console.Error.WriteLine($"File does not exist: {task.Filename}");
                                            await subscription.Ack(message, false);
                                        }
                                        break;

                                    case ImportTask.TaskTypes.DeleteFolders:
                                        Console.Error.WriteLine($"Deleting folder {folderIdentifier}");
                                        await API.Folder.DeleteAsync(folderIdentifier);
                                        await subscription.Ack(message, true);
                                        break;

                                }
                            }
                            catch (Exception e)
                            {
                                Console.Error.WriteLine("Exception: " + e);
                                await subscription.Ack(message, false);
                            }
                        });
                    }
                }
            }
        }
    }
}
