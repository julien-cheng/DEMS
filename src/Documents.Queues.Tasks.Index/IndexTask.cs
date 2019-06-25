namespace Documents.Queues.Tasks.Index
{
    using Documents.API.Common.Exceptions;
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.API.Common.Models.MetadataModels;
    using Documents.Common;
    using Documents.Queues.Messages;
    using Documents.Queues.Tasks.Configuration;
    using Documents.Search;
    using Documents.Search.ElasticSearch;
    using Microsoft.Extensions.Logging;
    using MoreLinq;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    class IndexTask : 
        QueuedApplication<IndexTask.IndexConfiguration, IndexMessage>
    {
        protected override string ConfigurationSectionName => "DocumentsQueuesTasksIndex";
        protected override string QueueName => "Index";
        private const int MAX_SOURCE_FILE_SIZE = 1024 * 1025 * 10; // 10 mb
        private const int QUERY_PAGE_SIZE = 25000;
        private const int ENQUEUE_BATCH_SIZE = 2500;

        private ISearch Driver = null;

        protected override async Task Process()
        {
            if (Driver == null)
                Driver = new ElasticSearchDriver(
                    Logging.CreateLogger<ElasticSearchDriver>(),
                    Configuration.ElasticSearchUri,
                    Configuration.Index
                ) as ISearch;

            switch (CurrentMessage.Action)
            {
                case IndexMessage.IndexActions.IndexFile:

                    if (CurrentMessage.FolderModel != null && CurrentMessage.FileModel != null)
                        await IndexFile(Driver, CurrentMessage.FileModel, CurrentMessage.FolderModel);
                    else
                        await IndexFile(Driver, CurrentMessage.Identifier as FileIdentifier);
                    break;

                case IndexMessage.IndexActions.DeleteFile:
                    await DeleteFile(Driver, CurrentMessage.Identifier as FileIdentifier);
                    break;

                case IndexMessage.IndexActions.IndexFolder:
                    await IndexFolder(Driver, CurrentMessage.Identifier as FolderIdentifier);
                    break;

                case IndexMessage.IndexActions.DeleteFolder:
                    await DeleteFolder(Driver, CurrentMessage.Identifier as FolderIdentifier);
                    break;

                case IndexMessage.IndexActions.IndexOrganization:
                    await IndexOrganization(Driver, CurrentMessage.Identifier as OrganizationIdentifier);
                    break;

                case IndexMessage.IndexActions.DeleteOrganization:
                    await DeleteOrganization(Driver, CurrentMessage.Identifier as OrganizationIdentifier);
                    break;

                case IndexMessage.IndexActions.DeleteEntireIndex:
                    await DeleteEntireIndex(Driver);
                    break;

            }
        }

        private async Task DeleteEntireIndex(ISearch driver)
        {
            await driver.DeleteEntireIndex();
        }

        private async Task DeleteFolder(ISearch driver, FolderIdentifier folderIdentifier)
        {
            await driver.DeleteFolderAsync(API.UserAccessIdentifiers, folderIdentifier);
        }

        private async Task DeleteOrganization(ISearch driver, OrganizationIdentifier organizationIdentifier)
        {
            await driver.DeleteOrganizationAsync(API.UserAccessIdentifiers, organizationIdentifier);
        }

        private async Task DeleteFile(ISearch driver, FileIdentifier fileIdentifier)
        {
            await driver.DeleteFileAsync(API.UserAccessIdentifiers, fileIdentifier);
        }

        private async Task IndexFile(ISearch driver, FileIdentifier fileIdentifier)
        {
            var fileModel = await GetFileAsync(CurrentMessage.Identifier as FileIdentifier);
            if (fileModel == null)
                await driver.DeleteFileAsync(API.UserAccessIdentifiers, fileIdentifier);
            else
            {
                var folderModel = await API.Folder.GetAsync(fileModel.Identifier as FolderIdentifier);

                await IndexFile(driver, fileModel, folderModel);
            }
        }

        private async Task IndexFile(ISearch driver, FileModel fileModel, FolderModel folderModel)
        {
            // if this file is a child, index the parent instead
            var childOf = fileModel.Read<FileIdentifier>(MetadataKeyConstants.CHILDOF);
            if (childOf != null)
            {
                var parentFileModel = await API.File.GetAsync(childOf);
                if (parentFileModel != null)
                {
                    await IndexFile(driver, parentFileModel, folderModel);
                    return;
                }
            }

            var text = new StringBuilder();

            if (!fileModel.Read<bool>(MetadataKeyConstants.HIDDEN, defaultValue: false))
            {
                var alternativeViews = fileModel.Read<List<AlternativeView>>(MetadataKeyConstants.ALTERNATIVE_VIEWS);

                var textIdentifiers = alternativeViews?
                    .Where(a => 
                        a.MimeType == "text/plain"
                        || a.MimeType == "text/vtt"
                    )
                    .Select(a => a.FileIdentifier)
                    .ToList()
                    ?? new List<FileIdentifier>();

                if (fileModel.MimeType == "text/plain" && fileModel.Length < MAX_SOURCE_FILE_SIZE)
                    textIdentifiers.Add(fileModel.Identifier);

                if (textIdentifiers.Any())
                    foreach(var identifier in textIdentifiers)
                        using (var ms = new MemoryStream())
                        {
                            try
                            {
                                await API.File.DownloadAsync(identifier, ms);
                                text.AppendLine(Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length));
                            }
                            catch (DocumentsNotFoundException)
                            {
                                Logger.LogWarning("Not Found while downloading text contents");
                            }
                        }

                await driver.IndexFileAsync(
                    folderModel,
                    fileModel,
                    text.ToString());
            }
        }

        private async Task IndexFolder(ISearch driver, FolderIdentifier folderIdentifier)
        {
            var pageIndex = 0;
            var done = false;

            await driver.DeleteFolderAsync(API.UserAccessIdentifiers, folderIdentifier);

            while (!done)
            {
                var folderModel = await API.Folder.GetAsync(folderIdentifier, new List<PopulationDirective>
                {
                    new PopulationDirective(nameof(FolderModel.Files))
                    {
                        Paging = new PagingArguments
                        {
                            PageSize = QUERY_PAGE_SIZE,
                            PageIndex = pageIndex
                        }
                    }
                });

                if (folderModel != null)
                {
                    var files = folderModel.Files.Rows;

                    folderModel.Files.Rows = null; // decreasing the size of the next messages

                    foreach (var batch in files.Batch(ENQUEUE_BATCH_SIZE))
                        await API.Queue.EnqueueAsync(batch
                            .Where(f => !f.Read<bool>(MetadataKeyConstants.HIDDEN, defaultValue: false))
                            .Select(file => new QueuePair
                            {
                                QueueName = "Index",
                                Message = JsonConvert.SerializeObject(new IndexMessage
                                {
                                    Action = IndexMessage.IndexActions.IndexFile,
                                    Identifier = file.Identifier,
                                    FolderModel = folderModel,
                                    FileModel = file
                                })
                            }));

                    pageIndex++;
                    done = files.Count() < QUERY_PAGE_SIZE;
                }
                else
                    done = true;
            }
        }



        private async Task IndexOrganization(ISearch driver, OrganizationIdentifier organizationIdentifier)
        {
            var pageIndex = 0;
            var done = false;

            await driver.DeleteOrganizationAsync(API.UserAccessIdentifiers, organizationIdentifier);

            while (!done)
            {
                var organization = await API.Organization.GetAsync(organizationIdentifier, new List<PopulationDirective>
                {
                    new PopulationDirective
                    {
                        Name = nameof(OrganizationModel.Folders),
                        Paging = new PagingArguments
                        {
                            PageSize = QUERY_PAGE_SIZE,
                            PageIndex = pageIndex
                        }
                    }
                });

                if (organization != null)
                {
                    foreach (var batch in organization.Folders.Rows.Batch(ENQUEUE_BATCH_SIZE))
                        await API.Queue.EnqueueAsync(batch.Select(folder => new QueuePair
                        {
                            QueueName = "Index",
                            Message = JsonConvert.SerializeObject(new IndexMessage
                            {
                                Action = IndexMessage.IndexActions.IndexFolder,

                                // queue expects a null in the FileKey to indicated a FolderIdentifier
                                // todo: weak typing
                                Identifier = new FileIdentifier(folder.Identifier, null)
                            })
                        }));

                    pageIndex++;
                    done = organization.Folders.Rows.Count() < QUERY_PAGE_SIZE;
                }
                else
                    done = true;
            }
        }

        public class IndexConfiguration : TaskConfiguration
        {
            public Uri ElasticSearchUri { get; set; }
            public string Index { get; set; }

            public override string SectionName => "DocumentsQueuesTasksIndex";

            public IndexConfiguration()
            {
                this.LogCompletion = false;
            }
        }
    }
}
