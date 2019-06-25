namespace Documents.Clients.Tools.Commands.File
{
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.API.Common.Models.MetadataModels;
    using McMaster.Extensions.CommandLineUtils;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    [Subcommand("get", typeof(Get))]
    [Subcommand("delete", typeof(Delete))]
    [Subcommand("download", typeof(Download))]
    [Subcommand("upload", typeof(Upload))]
    [Subcommand("list", typeof(List))]
    [Subcommand("metadata", typeof(FileMetadataCommand))]
    [Subcommand("tag", typeof(Tag))]
    [Subcommand("status", typeof(Status))]
    class FileCommand : CommandBase
    {
        public void RenderFile(FileModel model, bool showPrivileges = false)
        {
            if (model != null)
            {
                Table("File", new
                {
                    model.Identifier.OrganizationKey,
                    model.Identifier.FolderKey,
                    model.Identifier.FileKey,
                    model.Name,
                    model.MimeType,
                    model.LengthForHumans
                });

                if (showPrivileges)
                {
                    RenderPrivileges("file", model.FilePrivileges);
                }
            }
            else
            {
                throw new System.Exception("File does not exist");
            }
        }

        class Get : CommandBase
        {
            [Argument(0, Description = "Key")]
            public string Key { get; }

            [Option]
            public bool Privileges { get; } = false;

            protected async override Task ExecuteAsync()
            {
                var FileIdentifier = GetFileIdentifier(Key);

                var model = await API.File.GetOrThrowAsync(FileIdentifier);

                GetParent<FileCommand>().RenderFile(model, Privileges);
            }
        }

        class Delete : CommandBase
        {
            [Argument(0, Description = "Key")]
            public string Key { get; }

            [Option]
            public bool SpareTheChildren { get; }

            protected async override Task ExecuteAsync()
            {
                var fileIdentifier = GetFileIdentifier(Key);

                var model = await API.File.DeleteAsync(fileIdentifier);

                if (!SpareTheChildren)
                {
                    foreach (var view in model.Read(MetadataKeyConstants.ALTERNATIVE_VIEWS, defaultValue: new List<AlternativeView>()))
                    {
                        await API.File.DeleteAsync(view.FileIdentifier);
                    }
                }
            }
        }

        class List : CommandBase
        {
            [Argument(0, Description = "Key")]
            public string Key { get; }

            protected async override Task ExecuteAsync()
            {
                var folderIdentifier = GetFolderIdentifier(Key);

                var model = await API.Folder.GetOrThrowAsync(folderIdentifier,
                    new[] { new PopulationDirective(nameof(FolderModel.Files)) });

                Table("Files", model.Files.Rows.Select(f =>
                 new
                 {
                     f.Identifier.OrganizationKey,
                     f.Identifier.FolderKey,
                     f.Identifier.FileKey,
                     f.Name,
                     f.MimeType,
                     f.LengthForHumans
                 }));
            }
        }

        class Download : CommandBase
        {
            [Argument(0, Description = "Key")]
            public string Key { get; }

            [Option]
            public string Output { get; }

            protected async override Task ExecuteAsync()
            {
                var fileIdentifier = GetFileIdentifier(Key);
                var model = await API.File.GetAsync(fileIdentifier);

                string outputPath = null;
                var fileName = model.Name;
                // make safe

                if (Output != null)
                {
                    if (Directory.Exists(Output))
                    {
                        outputPath = Path.Combine(Output, fileName);
                    }
                    else
                    {
                        outputPath = Output;
                    }
                }
                else
                {
                    outputPath = Path.Combine(Directory.GetCurrentDirectory(), fileName);
                }

                using (var fileStream = new FileStream(outputPath, FileMode.OpenOrCreate))
                {
                    await API.File.DownloadAsync(fileIdentifier, fileStream);
                }

                GetParent<FileCommand>().RenderFile(model);
            }

        }

        class Upload : CommandBase
        {
            [Argument(0, Description = "Key")]
            public string Key { get; }

            [Argument(1)]
            public string Filename { get; }

            protected async override Task ExecuteAsync()
            {
                var fileIdentifier = GetFileIdentifier(Key);
                var model = await API.File.FileModelFromLocalFileAsync(Filename, fileIdentifier);

                using (var fileStream = new FileStream(Filename, FileMode.Open))
                {
                    model = await API.File.UploadAsync(model, fileStream);
                }

                GetParent<FileCommand>().RenderFile(model);
            }
        }

        class Tag : CommandBase
        {
            [Argument(0, Description = "Key")]
            public string Key { get; }

            [Argument(1, Description = "Tag Key")]
            public string TagKey { get; }

            [Argument(2, Description = "Value")]
            public string Value { get; }

            protected async override Task ExecuteAsync()
            {
                var fileIdentifier = GetFileIdentifier(Key);

                if (Value != null)
                {
                    await API.File.SetTagsAsync(
                        fileIdentifier,
                        new Dictionary<string, string>
                        {
                            { TagKey, Value == "null" ? null : Value }
                        }
                    );
                }

                var tags = await API.File.GetTagsAsync(fileIdentifier);

                Table("Tags", tags.Select(t => new { t.Key, t.Value }));
            }
        }

        class Status : CommandBase
        {
            [Argument(0, Description = "Key")]
            public string Key { get; }

            protected async override Task ExecuteAsync()
            {
                var fileIdentifier = GetFileIdentifier(Key);

                var status = await API.File.GetOnlineStatusAsync(fileIdentifier);

                Table("Status", new { Status = status.ToString() });
            }
        }
    }
}
