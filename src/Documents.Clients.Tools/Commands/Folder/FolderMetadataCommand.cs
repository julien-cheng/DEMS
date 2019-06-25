namespace Documents.Clients.Tools.Commands.Folder
{
    using Documents.API.Common.MetadataPersistence;
    using McMaster.Extensions.CommandLineUtils;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    [Subcommand("get", typeof(FolderGet))]
    [Subcommand("set", typeof(FolderSet))]
    class FolderMetadataCommand : ModelMetadataCommandBase
    {
        class FolderGet : MetadataGetBase
        {
            [Option("--all")]
            public bool All { get; }

            [Option("--file")]
            public bool File { get; }

            [Argument(0)]
            public string FolderIdentifier { get; }

            public FolderGet()
            {
                this.Handler = new FolderHandler
                {
                    Get = this
                };
            }
        }

        class FolderSet : MetadataSetBase
        {
            [Option("--all")]
            public bool All { get; }

            [Option("--file")]
            public bool File { get; }

            [Argument(0)]
            public string FolderIdentifier { get; }

            public FolderSet()
            {
                this.Handler = new FolderHandler
                {
                    Set = this
                };
            }
        }

        class FolderHandler : IMetadataHandler
        {
            public FolderGet Get { get; set; }
            public FolderSet Set { get; set; }

            async Task IMetadataHandler.PopulateRows(List<MetadataRow> rows)
            {
                var folderIdentifier = Get.GetFolderIdentifier(Get.FolderIdentifier);

                var model = await Get.API.Folder.GetOrThrowAsync(folderIdentifier);

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
                var folderIdentifier = Set.GetFolderIdentifier(Set.FolderIdentifier);

                var model = await Set.API.Folder.GetOrThrowAsync(folderIdentifier);

                object objValue = Set.GetObjectValue(Set.Value);

                if (Set.File)
                {
                    Console.WriteLine("Writing for file");
                    model.WriteForFile(Set.Key, objValue);
                }
                else
                    model.Write(Set.Key, objValue);

                await Set.API.Folder.PutAsync(model);
            }
        }
    }
}
