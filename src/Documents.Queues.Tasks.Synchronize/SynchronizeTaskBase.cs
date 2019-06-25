namespace Documents.Queues.Tasks.Synchronize
{
    using Documents.API.Common.MetadataPersistence;
    using Documents.API.Common.Models;
    using Documents.API.Common.Security;
    using Documents.Queues.Messages;
    using Documents.Queues.Tasks.Configuration;
    using Documents.Queues.Tasks.Synchronize.MetadataModels;
    using Microsoft.Extensions.Caching.Memory;
    using MoreLinq;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;

    public abstract class SynchronizeTaskBase :
        QueuedApplication<SynchronizeTask.ConfigurationType, SynchronizeMessage>
    {
        protected override string ConfigurationSectionName => "DocumentsQueuesTasksSynchronize";

        private const int ENQUEUE_BATCH_SIZE = 250;

        protected SynchronizeConfiguration SynchronizeConfiguration;

        private static MemoryCache ConfigurationCache = new MemoryCache(new MemoryCacheOptions());

        protected async Task LoadConfiguration()
        {
            if (CurrentMessage.ConfigurationHash != null)
            {
                if (ConfigurationCache.TryGetValue(CurrentMessage.ConfigurationHash, out object cached))
                {
                    SynchronizeConfiguration = cached as SynchronizeConfiguration;
                    return;
                }
            }

            var privateFolder = await API.Folder.GetAsync(new FolderIdentifier(CurrentMessage.OrganizationIdentifier, ":private"));
            SynchronizeConfiguration = privateFolder.Read<SynchronizeConfiguration>(SynchronizeConfiguration.METADATA_KEY);

            string json = privateFolder.MetadataFlattened.ReadRaw(SynchronizeConfiguration.METADATA_KEY);
            using (var md5 = MD5.Create())
            {
                var result = md5.ComputeHash(Encoding.ASCII.GetBytes(json));

                // set the hash for the new one
                var newHash = Convert.ToBase64String(result);
                ConfigurationCache.Set(newHash, SynchronizeConfiguration);

                // if the old hash is different from the new one, we
                // might as well cache it anyways, since we know it's current
                // and the message might be stale
                if (CurrentMessage.ConfigurationHash != null
                    && CurrentMessage.ConfigurationHash != newHash)
                {
                    ConfigurationCache.Set(CurrentMessage.ConfigurationHash, SynchronizeConfiguration);
                }
                CurrentMessage.ConfigurationHash = newHash;
            }
        }

        protected async Task SaveConfiguration()
        {
            var privateFolder = await API.Folder.GetAsync(new FolderIdentifier(CurrentMessage.OrganizationIdentifier, ":private"));
            privateFolder.Write(SynchronizeConfiguration.METADATA_KEY, SynchronizeConfiguration);
            await API.Folder.PutAsync(privateFolder);
        }

        protected async Task Reader(
            string sql,
            Dictionary<string, object> args,
            Func<SqlDataReader, Task> needsConnection
        )
        {
            using (var connection = new SqlConnection())
            {
                connection.ConnectionString = SynchronizeConfiguration.ConnectionString;
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    command.CommandTimeout = 60;
                    if (args != null)
                        command.Parameters.AddRange(args.Select(a => new SqlParameter
                        {
                            ParameterName = a.Key,
                            Value = a.Value ?? DBNull.Value
                        }).ToArray());

                    using (var reader = await command.ExecuteReaderAsync())
                        await needsConnection(reader);
                }
            }
        }

        protected async Task QueueComponents(string queue, string component, IEnumerable<int> keys)
        {
            foreach (var batch in keys.Batch(ENQUEUE_BATCH_SIZE))
            {
                await API.Queue.EnqueueAsync(batch.Select(id => new QueuePair
                {
                    QueueName = queue,
                    Message = JsonConvert.SerializeObject(new SynchronizeMessage
                    {
                        Component = component,
                        Key = id,
                        OrganizationIdentifier = CurrentMessage.OrganizationIdentifier,
                        ConfigurationHash = CurrentMessage.ConfigurationHash
                    })
                }));
            }
        }

        protected async Task QueueComponent(string queue, string component, int key)
        {
            await API.Queue.EnqueueAsync(queue, new SynchronizeMessage
            {
                Component = component,
                Key = key,
                OrganizationIdentifier = CurrentMessage.OrganizationIdentifier,
                ConfigurationHash = CurrentMessage.ConfigurationHash
            });
        }

        public class ConfigurationType : TaskConfiguration
        {
            public string QueueName { get; set; } = "Synchronize";
            public string QueueNameManager { get; set; } = "SynchronizeManager";

            public override string SectionName => "DocumentsQueuesTasksSynchronize";
            public int ScheduleEntryTimeout { get; set; } = 30000;
        }
    }
}
