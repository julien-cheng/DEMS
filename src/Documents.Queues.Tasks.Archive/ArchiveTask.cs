namespace Documents.Queues.Tasks.Archive
{
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.Queues.Messages;
    using Documents.Queues.Tasks.Archive.Configuration;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Threading.Tasks;

    class ArchiveTask :
        QueuedApplication<ArchiveConfiguration, ArchiveMessage>
    {
        protected override string ConfigurationSectionName => "DocumentsQueuesTasksArchive";
        protected override string QueueName => "Archive";

        // we're going to extract a zip file
        protected override async Task Process()
        {
            var archiveFileModel = await base.GetFileAsync();
            // download the file and get the file model for the original archive
            await base.DownloadAsync();

            // create a directory (cleanup handled by base class, CreateTemporaryFile is tracked)
            var tempDirectory = CreateTemporaryFile();
            Directory.CreateDirectory(tempDirectory);

            // extract the zip into that director
            ZipFile.ExtractToDirectory(LocalFilePath, tempDirectory);

            // for every file in the newly extracted path
            foreach (var fileName in Directory.EnumerateFiles(tempDirectory, "*.*", SearchOption.AllDirectories))
            {
                // if path is /tmp/tmp-guid-123123/subfolder/file.ext, we want just subfolder/file.ext
                var relative = fileName.Substring(tempDirectory.Length);
                var path = Path.GetDirectoryName(relative);

                // stay os-neutral. split the file-based on system's directory separator
                // windows=backslash other=slash
                // then recombine it into a DMS path, with forward slashes
                path = String.Join("/", path.Split(Path.DirectorySeparatorChar));

                // no leading slash in our path name
                while (path.StartsWith("/"))
                    path = path.Substring(1);

                // read the dms path of the original archive file
                var archivePath = archiveFileModel.Read<string>("_path");

                // build a new path from
                var parts = new List<string>
                {
                    archivePath, // dms archive path
                    archiveFileModel.Name, // a subfolder with the same name as the archive filename 
                    path // the relative path of the file within the zip
                };
                path = String.Join("/", parts.Where(p => !string.IsNullOrWhiteSpace(p)));

                // upload the new file
                var newFileModel = await API.File.FileModelFromLocalFileAsync(fileName, new FileIdentifier(archiveFileModel.Identifier, null));
                newFileModel.Write<string>("_path", path);

                // upload the file
                await API.File.UploadLocalFileAsync(fileName, newFileModel);
            }

            // we're done
        }
    }
}
