namespace Documents.Clients.Manager.Services
{
    using Common;
    using Documents.API.Common.Models;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class LogReaderService
    {
        private readonly APIConnection connection;
        public LogReaderService(APIConnection connection)
        {
            this.connection = connection;
        }

        public async Task<PagedResults<AuditLogEntryModel>> ReadLog(FolderIdentifier folderIdentifier)
        {
            var logs = await connection.Log.LoadAsync(new[]
            {
                new PopulationDirective
                {
                    MetadataFilter = new List<MetadataMatchModel>
                    {
                        new MetadataMatchModel
                        {
                            Name = "OrganizationKey",
                            Operator = "==",
                            Value = folderIdentifier.OrganizationKey
                        },
                        new MetadataMatchModel
                        {
                            Name = "FolderKey",
                            Operator = "==",
                            Value = folderIdentifier.FolderKey
                        },
                        new MetadataMatchModel
                        {
                            Name = "InternalOnly",
                            Operator = "==",
                            Value = "true"
                        }
                    }
                }
            });

            return logs;
        }
    }
}