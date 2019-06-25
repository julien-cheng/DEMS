# Document.Queues.Tasks.Synchronization

This service synchronizes PCMS's database to DMS.

It's configured via appsettings.json in the same path and installed as a service named "Documents.Queues.Tasks.Synchronize" (running as PCMSWeb). The service needs to run as a user that has integrated security rights with SQL Server so that it can connect to the PCMS database. The appsettings.json config contains only information about how to connect/authenticate to the dms api.

It can be installed/uninstalled as a service from cmd.exe prompt (as administrator)
`dotnet Documents.Queues.Tasks.Synchronize.dll action:install`
`dotnet Documents.Queues.Tasks.Synchronize.dll action:uninstall`

Note, this will set run as account to local system and set it to auto start with windows.

It logs to a rolling set of text files in the install path with the date, such as debug-20180417.txt
Log location and verbosity can be controlled via appsettings.json or the standard system DOCUMENTS_CONFIG_PATH environment variable.

the dms system itself has configuration information for the PCMS connection strings per county. So, when we get to the shareded counties in production, we'll use separate configuration/connection info for each pcms-county/dms-organization. 

this service only ever reads from PCMS, and a readonly account is possible (at least today).  The service can be safely killed and restarted. It should recover fully and remain syncrhonized.

this service is simply an agent of the dms queue. I can be called upon to start complete harvests of county data, or to simply do delta syncrhonization on either a per county or system-wide basis.

```dms sync --help
Usage: Documents.Clients.Tools sync [arguments] [options]

Arguments:
  OrganizationIdentifier

Options:
  --help                  Show help information
  -c|--complete           Complete
  -w|--wait               Wait
  -s|--sync-all           SyncAll
  -t|--timeout <TIMEOUT>  Timeout
  -e|--every <EVERY>      Every
```

Eventually, this will be running all the time from within kube
```dms sync --sync-all --wait --timeout 10000 --every 2500```

this will tell each organization that's configured for synchronization to perform a delta update. 
--sync-all means every organization should get a --sync task
altneratively use specify Organization identifier.

`--every` says send it every X ms.

```dms sync --complete PCMS:259``` will queue a full synchronization of all defendants and all accounts in the Training county. this can safely be run while deltas are running. it won't cause trouble with the scheduling, but it WILL be on the same priority execution as the deltas. So if you queue up a ton of full syncs during the day, it will stall the delta queue until they are complete.

the DMS-side metadata config is in the county's :private folder:
```dms folder metadata get PCMS:259/:private synchronize
{
  "ConnectionString": "Server=pcmstestdb.nypti.org; Integrated Security=sspi;initial catalog=PCMS; max pool size=12;",
  "CountyID": 259,
  "LastChangeLogID": 41195969,
  "LastAccountChangeLogID": 103844
}
```

The ChangeLogIDs are how progress is tracked. resetting them to zero will cause a full-sync on next delta request.

This sync agent queries data from PCMS with the following SQL Server functions:
ufDMSSynchronizeAccount
ufDMSSynchronizeDefendant

those values get mapped to folders:
```dms folder metadata get PCMS:259/Defendant:17843384
Source        Target  Key                             Value
---------------------------------------------------------------------------------------------------------------------------------
folder        folder  attribute.defendantid           17843384
folder        folder  attribute.firstname             "12345"
folder        folder  attribute.lastname              "67890"
folder        folder  attribute.casenumber            "201500066"
folder        folder  attribute.casestatus            "Closed"
folder        folder  attribute.closed                true
folder        folder  attribute.deleted               false
folder        folder  attribute.defense.first         "Marcus"
folder        folder  attribute.defense.last          "Woll"
organization  folder  attributelocators[locatorlist]  [{"key": "attribute.created",\n    "label": "EXIF Created",\n    "isIndexed
organization  folder  ediscovery[isactive]            true
organization  folder  _events                         [\n  {\n    "$type": "Documents.API.Common.EventHooks.EventQueueManager, Do
organization  folder  _imagegen[options]              [\n  {\n    "Format": "PNG",\n    "MaxHeight": 100\n  }\n]
```

the sproc also spits out all the security information:
`(basic) g:ViewOwn{97347} g:ViewOwn{98738} g:ViewOwn{109367} g:ViewOwn{110308} g:ViewAll 	NULL	NULL	(default) o:PCMS:259 u:system	(basic) g:EditOwn{97347} g:EditOwn{98738} g:EditOwn{109367} g:EditOwn{110308} g:EditAll	(default) o:PCMS:259 u:system`

rendered as DMS ACLs (Access Control Lists)

so whenever a folder is synchronized, its permissions are updated too
