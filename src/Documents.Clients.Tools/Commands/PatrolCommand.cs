namespace Documents.Clients.Tools.Commands
{
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

    [Subcommand("drone", typeof(Drone))]
    [Subcommand("folders", typeof(Folders))]
    class PatrolCommand : CommandBase
    {
        class Folders : CommandBase
        {
            [Required, Argument(0)]
            public string OrganizationIdentifier { get; }

            protected async override Task ExecuteAsync()
            {
                var organizationIdentifier = GetOrganizationIdentifier(OrganizationIdentifier);

                var paging = new PagingArguments
                {
                    PageSize = 10
                };

                var directives = new[] {
                    new PopulationDirective(nameof(OrganizationModel.Folders))
                    {
                        Paging = paging
                    }
                };

                var model = await API.Organization.GetOrThrowAsync(organizationIdentifier, directives);

                int count = 0;
                long total = model.Folders.TotalMatches;

                bool done = false;
                while (!done)
                {
                    foreach (var batch in model.Folders.Rows.Batch(1000))
                    {
                        await API.Queue.EnqueueAsync("Patrol", batch);
                        Console.WriteLine($"Enqueued {count}/{total} [{Math.Floor(1.0 * count / total * 100)}%]");
                    }

                    var newRows = model.Folders.Rows.Count();
                    count += newRows;
                    if (newRows > 0 && count < total)
                    {
                        paging.PageIndex++;
                        model = await API.Organization.GetOrThrowAsync(organizationIdentifier, directives);
                    }
                    else
                        done = true;
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
                    using (var subscription = await API.Queue.SubscribeAsync("Patrol"))
                    {
                        await subscription.ListenAsync(async (message) =>
                        {
                            try
                            {

                                var task = JsonConvert.DeserializeObject<PatrolTask>(message.Message);

                                if (Drain)
                                {
                                    await subscription.Ack(message, true);
                                    return;
                                }

                                Console.WriteLine($"Patroling {task.FolderKey}");

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
