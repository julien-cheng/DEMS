namespace Documents.Clients.Tools.Commands.Folder
{
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.Clients.Tools.Common;
    using McMaster.Extensions.CommandLineUtils;
    using Newtonsoft.Json;
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    [Subcommand("get", typeof(Get))]
    [Subcommand("create", typeof(Create))]
    [Subcommand("list", typeof(List))]
    [Subcommand("metadata", typeof(FolderMetadataCommand))]
    [Subcommand("sync", typeof(Sync))]
    [Subcommand("delete", typeof(Delete))]
    [Subcommand("compare", typeof(Compare))]
    class FolderCommand : CommandBase
    {
        public void RenderFolder(FolderModel model, bool showPrivileges = false)
        {
            if (model != null)
            {
                Table("Folder", new
                {
                    model.Identifier.OrganizationKey,
                    model.Identifier.FolderKey,
                });

                if (showPrivileges)
                {
                    RenderPrivileges("folder", model.FolderPrivileges);
                    RenderPrivileges("file", model.FilePrivileges);
                }
            }
            else
                throw new System.Exception("Folder does not exist");
        }

        class Get : CommandBase
        {
            [Argument(0, Description = "Key")]
            public string Key { get; }

            [Option]
            public bool Privileges { get; } = false;

            protected async override Task ExecuteAsync()
            {
                var folderIdentifier = GetFolderIdentifier(Key);

                var model = await API.Folder.GetOrThrowAsync(folderIdentifier);

                GetParent<FolderCommand>().RenderFolder(model, Privileges);
            }
        }

        class Delete : CommandBase
        {
            [Argument(0, Description = "Key")]
            public string Key { get; }

            protected async override Task ExecuteAsync()
            {
                var folderIdentifier = GetFolderIdentifier(Key);

                var model = await API.Folder.DeleteAsync(folderIdentifier);
            }
        }

        class Create : CommandBase
        {
            [Argument(0, Description = "Key")]
            public string Key { get; }

            protected async override Task ExecuteAsync()
            {
                var folderIdentifier = GetFolderIdentifier(Key);

                var model = await API.Folder.PutAsync(new FolderModel
                {
                    Identifier = folderIdentifier
                });

                GetParent<FolderCommand>().RenderFolder(model, false);
            }
        }

        class List : CommandBase
        {
            [Argument(0, Description = "Key")]
            public string Key { get; }

            protected async override Task ExecuteAsync()
            {
                var organizationIdentifier = GetOrganizationIdentifier(Key);

                var model = await API.Organization.GetOrThrowAsync(organizationIdentifier,
                    new[] { new PopulationDirective(nameof(OrganizationModel.Folders)) });

                Table("Folders", model.Folders.Rows.Select(f =>
                    f.Identifier));
            }
        }

        class Compare : CommandBase
        {

            class FileComparison
            {
                public string FileKey { get; set; }
                public long Length { get; set; }
                public DateTime? Modified { get; set; }
                public string MD5 { get; set; }
            }

            [Argument(0, Description = "Key")]
            public string Key { get; }

            [Argument(1, Description = "Local path to syncrhonize with")]
            public string Path { get; }

            protected async override Task ExecuteAsync()
            {
                var folderIdentifier = GetFolderIdentifier(Key);

                var folder = await API.Folder.GetOrThrowAsync(folderIdentifier, new[]
                {
                    new PopulationDirective(nameof(FolderModel.Files))
                });


                var remoteFiles = folder.Files.Rows.Select(f => new FileComparison
                {
                    FileKey = f.Identifier.FileKey,
                    Length = f.Length,
                    Modified = f.Modified,
                    MD5 = f.HashMD5
                });

                var remoteFilesByKey = remoteFiles.ToDictionary(f => f.FileKey, f => f);

                Table("Remote", remoteFiles);

                var rootPath = System.IO.Path.GetFullPath(Path);

                Table("Local",
                    Directory.EnumerateFileSystemEntries(rootPath, "*.*", SearchOption.AllDirectories)
                        .Where(f => File.Exists(f))
                        .Select(f =>
                        {
                            var i = new FileInfo(f);

                            var fullChangedPath = System.IO.Path.GetFullPath(i.FullName);
                            var fullDirectoryPath = System.IO.Path.GetDirectoryName(fullChangedPath);
                            var deltaPath = fullDirectoryPath.Length >= rootPath.Length && fullDirectoryPath.Substring(0, rootPath.Length).Equals(rootPath)
                                ? fullDirectoryPath.Substring(rootPath.Length)
                                : null;

                            if (deltaPath != null)
                            {
                                while (deltaPath.StartsWith(System.IO.Path.DirectorySeparatorChar))
                                    deltaPath = deltaPath.Substring(1);

                                deltaPath = deltaPath.Replace(System.IO.Path.DirectorySeparatorChar, '/');
                            }


                            string fileKey = null;

                            if (!string.IsNullOrWhiteSpace(deltaPath))
                            {
                                var parts = deltaPath.Split().ToList();
                                parts.Add(i.Name);

                                fileKey = String.Join("/", parts.ToArray());
                            }
                            else
                                fileKey = i.Name;

                            FileComparison remote = remoteFilesByKey.ContainsKey(fileKey)
                                ? remoteFilesByKey[fileKey]
                                : null;

                            return new
                            {
                                FileKey = fileKey,
                                i.Name,
                                Modified = i.LastWriteTimeUtc,
                                i.Length,
                                deltaPath,
                                remote = JsonConvert.SerializeObject(remote)
                            };
                        })
                );

            }
        }

        class Sync : CommandBase
        {
            [Argument(0, Description = "Key")]
            public string Key { get; }

            [Argument(1, Description = "Local path to syncrhonize with")]
            public string Path { get; }

            protected override Task ExecuteAsync()
            {
                var organizationIdentifier = GetOrganizationIdentifier(Key);

                using (var watcher = new DirectoryMonitor(System.IO.Path.GetDirectoryName(Path)))
                {
                    watcher.Change += Watcher_Change;
                    watcher.Start();

                    while (true)
                    {
                        Console.WriteLine("watching");
                        Console.ReadLine();
                    }
                }
            }

            private void Watcher_Change(string changedFilePath)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        var exists = File.Exists(changedFilePath);
                        var isDirectory = false;

                        try
                        {
                            isDirectory = ((File.GetAttributes(changedFilePath) & FileAttributes.Directory) == FileAttributes.Directory);
                        }
                        catch (Exception)
                        {
                            isDirectory = false;
                            exists = false;
                        }

                        var fullChangedPath = System.IO.Path.GetFullPath(changedFilePath);
                        var rootPath = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(Path));
                        var fullDirectoryPath = System.IO.Path.GetDirectoryName(fullChangedPath);
                        var deltaPath = fullDirectoryPath.Length >= rootPath.Length && fullDirectoryPath.Substring(0, rootPath.Length).Equals(rootPath)
                            ? fullDirectoryPath.Substring(rootPath.Length)
                            : null;

                        if (deltaPath != null)
                        {
                            while (deltaPath.StartsWith(System.IO.Path.DirectorySeparatorChar))
                                deltaPath = deltaPath.Substring(1);

                            deltaPath = deltaPath.Replace(System.IO.Path.DirectorySeparatorChar, '/');
                        }


                        if (!isDirectory)
                        {
                            if (exists)
                            {
                                var model = await API.File.FileModelFromLocalFileAsync(fullChangedPath,
                                    new FileIdentifier(GetFolderIdentifier(Key), System.IO.Path.Combine(deltaPath, System.IO.Path.GetFileName(changedFilePath)))
                                );
                                model.InitializeEmptyMetadata();

                                if (!string.IsNullOrWhiteSpace(deltaPath))
                                    model.Write("_path", deltaPath);

                                Console.WriteLine($"Uploading {fullChangedPath}");
                                using (var fileStream = new FileStream(fullChangedPath, FileMode.Open))
                                    await API.File.UploadAsync(model, fileStream);
                            }
                        }

                        Console.WriteLine(JsonConvert.SerializeObject(new
                        {
                            isDirectory,
                            exists,
                            deltaPath,
                            fileName = System.IO.Path.GetFileName(changedFilePath)
                        }, Formatting.Indented));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                });
            }
        }
    }
}
