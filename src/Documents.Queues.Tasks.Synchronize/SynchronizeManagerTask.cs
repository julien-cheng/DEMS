namespace Documents.Queues.Tasks.Synchronize
{
    using Documents.API.Common.MetadataPersistence;
    using Documents.Queues.Messages;
    using Documents.Queues.Tasks.Synchronize.MetadataModels;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;


    partial class SynchronizeManagerTask : SynchronizeTaskBase
    {
        protected override string QueueName => base.Configuration.QueueNameManager;

        protected override async Task Process()
        {
            switch (CurrentMessage.Component)
            {

                case "ScheduleEntry":
                    await ScheduleEntry();

                    break;

                default:
                    await LoadConfiguration();

                    switch (CurrentMessage.Component)
                    {
                        case "Complete":
                            await QueueAccounts(delta: false);
                            await QueueDefendants(delta: false);

                            break;

                        case "Delta":
                            await QueueAccounts(delta: true);
                            await QueueDefendants(delta: true);

                            break;

                        case "SetLastChangeLogID":
                            await SetLastChangeLogID(changeLogID: CurrentMessage.Key, accountChangeLogID: null);

                            break;

                        case "SetLastAccountChangeLogID":
                            await SetLastChangeLogID(changeLogID: null, accountChangeLogID: CurrentMessage.Key);

                            break;
                    }
                    break;
            }
        }

        private async Task ScheduleEntry()
        {
            var organizations = await API.Organization.GetAllAsync();

            foreach (var organization in organizations.Rows
                .Where(o => o.Read<bool>("synchronize[isActive]", defaultValue: false)))
            {
                var message = new SynchronizeMessage
                {
                    OrganizationIdentifier = organization.Identifier,
                    Component = "Delta"
                };

                var response = await API.Queue.EnqueueWithCallbackWait(Configuration.QueueNameManager, message, Configuration.ScheduleEntryTimeout);

                message = JsonConvert.DeserializeObject<SynchronizeMessage>(response.Message);
                this.CurrentMessage.Result = (this.CurrentMessage.Result ?? string.Empty)
                    + message.Result;
            }
        }

        private async Task QueueDefendants(bool delta)
        {
            if (!delta)
                Logger.LogInformation($"Harvesting entire set for CountyID: {SynchronizeConfiguration.CountyID}");
            else
                Logger.LogDebug($"Queueing Defendants for CountyID: {SynchronizeConfiguration.CountyID}");

            int queued = 0;
            int changeLogID = 0;

            var query = delta
                ? SQLDefendantsQueryDelta()
                : SQLDefendantsQueryAll();

            var defendants = new List<int>();

            await this.Reader(query,
                new Dictionary<string, object> {
                    { "ChangeLogID", SynchronizeConfiguration.LastChangeLogID ?? -1 },
                    { "CountyID", SynchronizeConfiguration.CountyID }
                },
                async reader =>
                {
                    changeLogID = SynchronizeConfiguration.LastChangeLogID ?? -1;

                    while (await reader.ReadAsync())
                    {
                        defendants.Add(reader.GetInt32(0));
                        changeLogID = Math.Max(changeLogID, reader.GetInt32(1));
                        queued++;
                    }
                }
            );

            await QueueComponents(Configuration.QueueName, "Defendant", defendants);

            if (changeLogID != SynchronizeConfiguration.LastChangeLogID)
                await SetLastChangeLogID(
                    changeLogID: changeLogID,
                    accountChangeLogID: null
                );

            if (queued > 0)
            {
                var outcome = $"Queued {queued} Defendants for CountyID: {SynchronizeConfiguration.CountyID}";
                Logger.LogInformation(outcome);
                if (CurrentMessage.Result == null)
                    CurrentMessage.Result = string.Empty;
                CurrentMessage.Result += outcome + "\n";
            }
        }

        private async Task QueueAccounts(bool delta)
        {

            int queued = 0;

            var query = delta
                ? SQLAccountsQueryDelta()
                : SQLAccountsQueryAll();

            await this.Reader(query,
                new Dictionary<string, object> {
                    { "ChangeLogAccountID", SynchronizeConfiguration.LastAccountChangeLogID ?? -1 },
                    { "CountyID", SynchronizeConfiguration.CountyID }
                },
                async reader =>
                {
                    int accountChangeLogID = SynchronizeConfiguration.LastAccountChangeLogID ?? -1;

                    while (await reader.ReadAsync())
                    {
                        await this.QueueComponent(Configuration.QueueName, "Account", reader.GetInt32(0));
                        accountChangeLogID = Math.Max(accountChangeLogID, reader.GetInt32(1));
                        queued++;

                    }

                    if (accountChangeLogID != SynchronizeConfiguration.LastAccountChangeLogID)
                        await SetLastChangeLogID(
                            changeLogID: null,
                            accountChangeLogID: accountChangeLogID
                        );
                }
            );

            if (queued > 0)
            {
                var outcome = $"Queued {queued} Accounts for CountyID: {SynchronizeConfiguration.CountyID}";
                Logger.LogInformation(outcome);
                if (CurrentMessage.Result == null)
                    CurrentMessage.Result = string.Empty;
                CurrentMessage.Result += outcome + "\n";
            }

        }


        private string SQLAccountsQueryDelta()
        {
            return @"
                    select distinct
	                    SSOAccountID,
                        Max(cla.ChangeLogAccountID) ChangeLogAccountID
                    from
	                    ChangeLogAccount cla
	                    inner join Account a
		                    on cla.AccountID=a.AccountID
                    where
	                    a.CountyID = @CountyID
	                    and a.SSOAccountID is not null
	                    and cla.ChangeLogAccountID > @ChangeLogAccountID
                    group by
                        SSOAccountID
                ";
        }

        private string SQLAccountsQueryAll()
        {
            return @"
                    select distinct
                        SSOAccountID,
                        (select max(ChangeLogAccountID) from ChangeLogAccount) ChangeLogAccountID
                    from
                        Account a
                    where
                        a.CountyID = @CountyID
                        and a.SSOAccountID is not null
                    group by
                        SSOAccountID
                ";
        }

        private string SQLDefendantsQueryDelta()
        {
            return @"select distinct
                        d.DefendantID,
                        Max(cl.ChangeLogID) ChangeLogID
                    from
                        ChangeLog cl
                        inner join Defendant d
                            on cl.DefendantID=d.DefendantID 
                    where
                        ChangeLogID > @ChangeLogID
                        and CountyID = @CountyID
                    group by
                        d.DefendantID
                    ";
        }

        private string SQLDefendantsQueryAll()
        {
            return @"select d.DefendantID,
                        (select max(ChangeLogID) from ChangeLog) ChangeLogID
                    from
                        Defendant d
                    where
                        CountyID = @CountyID
                    order by d.DefendantID desc
                    ";
        }


        private async Task SetLastChangeLogID(int? changeLogID, int? accountChangeLogID)
        {
            if (changeLogID.HasValue)
                SynchronizeConfiguration.LastChangeLogID = changeLogID;

            if (accountChangeLogID.HasValue)
                SynchronizeConfiguration.LastAccountChangeLogID = accountChangeLogID;

            await SaveConfiguration();
        }
    }
}
