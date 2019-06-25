namespace Documents.Clients.Tools.Commands.File
{
    using Documents.API.Common.MetadataPersistence;
    using McMaster.Extensions.CommandLineUtils;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    [Subcommand("get", typeof(FileGet))]
    [Subcommand("set", typeof(FileSet))]
    class FileMetadataCommand : ModelMetadataCommandBase
    {
        class FileGet : MetadataGetBase
        {
            [Argument(0)]
            public string FileIdentifier { get; }

            public FileGet()
            {
                this.Handler = new FileHandler
                {
                    Get = this
                };
            }
        }

        class FileSet : MetadataSetBase
        {
            [Argument(0)]
            public string FileIdentifier { get; }

            public FileSet()
            {
                this.Handler = new FileHandler
                {
                    Set = this
                };
            }
        }

        class FileHandler : IMetadataHandler
        {
            public FileGet Get { get; set; }
            public FileSet Set { get; set; }

            async Task IMetadataHandler.PopulateRows(List<MetadataRow> rows)
            {
                var FileIdentifier = Get.GetFileIdentifier(Get.FileIdentifier);

                var model = await Get.API.File.GetOrThrowAsync(FileIdentifier);

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
                var FileIdentifier = Set.GetFileIdentifier(Set.FileIdentifier);

                var model = await Set.API.File.GetOrThrowAsync(FileIdentifier);

                object objValue = Set.GetObjectValue(Set.Value);

                model.Write(Set.Key, objValue);

                await Set.API.File.PutAsync(model);
            }
        }
    }
}
