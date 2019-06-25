namespace Documents.Clients.Tools.Commands.Organization
{
    using Documents.API.Common.MetadataPersistence;
    using McMaster.Extensions.CommandLineUtils;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    [Subcommand("get", typeof(OrganizationGet))]
    [Subcommand("set", typeof(OrganizationSet))]
    [Command(Description ="operate on organization metadata")]
    class OrganizationMetadataCommand : ModelMetadataCommandBase
    {
        [Command(Description = "get an organization's metadata")]
        class OrganizationGet : MetadataGetBase
        {
            [Option("--all", Description = "include inheritable metadata for any object")]
            public bool All { get; }

            [Option("--folder", Description = "include inheritable metadata for folders")]
            public bool Folder { get; }

            [Option("--file", Description = "include inheritable metadata for files")]
            public bool File { get; }

            [Argument(0, Description = "OrganizationIdentifier")]
            public string OrganizationIdentifier { get; }

            public OrganizationGet()
            {
                this.Handler = new OrganizationHandler
                {
                    Get = this
                };
            }
        }

        class OrganizationSet : MetadataSetBase
        {
            [Option("--all")]
            public bool All { get; }

            [Option("--folder")]
            public bool Folder { get; }

            [Option("--file")]
            public bool File { get; }

            [Argument(0)]
            public string OrganizationIdentifier { get; }

            public OrganizationSet()
            {
                this.Handler = new OrganizationHandler
                {
                    Set = this
                };
            }
        }

        class OrganizationHandler : IMetadataHandler
        {
            public OrganizationGet Get { get; set; }
            public OrganizationSet Set { get; set; }

            async Task IMetadataHandler.PopulateRows(List<MetadataRow> rows)
            {
                var organizationIdentifier = Get.GetOrganizationIdentifier(Get.OrganizationIdentifier);

                var model = await Get.API.Organization.GetOrThrowAsync(organizationIdentifier);

                rows.AddRange(
                    model.OrganizationMetadata
                        .SelectMany(t =>
                            t.Value
                                .Select(m => new MetadataRow
                                {
                                    Target = "organization",
                                    Source = t.Key,
                                    Key = m.Key,
                                    Value = m.Value
                                })
                                .Where(o => Get.Key == null || o.Key == Get.Key)
                        )
                );

                if (Get.All || Get.Folder)
                    rows.AddRange(
                        model.FolderMetadata
                            .SelectMany(t =>
                                t.Value
                                    .Select(m => new MetadataRow
                                    {
                                        Target = "folder",
                                        Source = t.Key,
                                        Key = m.Key,
                                        Value = m.Value
                                    })
                                    .Where(o => Get.Key == null || o.Key == Get.Key)
                            )
                        );

                if (Get.All || Get.File)
                    rows.AddRange(
                        model.FileMetadata
                            .SelectMany(t =>
                                t.Value
                                    .Select(m => new MetadataRow
                                    {
                                        Target = "file",
                                        Source = t.Key,
                                        Key = m.Key,
                                        Value = m.Value
                                    })
                                    .Where(o => Get.Key == null || o.Key == Get.Key)
                            )
                        );
            }

            async Task IMetadataHandler.SetMetadata()
            {
                var organizationIdentifier = Set.GetOrganizationIdentifier(Set.OrganizationIdentifier);

                var model = await Set.API.Organization.GetOrThrowAsync(organizationIdentifier);

                object objValue = Set.GetObjectValue(Set.Value);

                if (Set.File)
                    model.WriteForFile(Set.Key, objValue);
                else if (Set.Folder)
                    model.WriteForFolder(Set.Key, objValue);
                else
                    model.Write(Set.Key, objValue);

                await Set.API.Organization.PutAsync(model);
            }
        }
    }
}
