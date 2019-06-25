# Documents.Clients.Tools - dms cli

### dms.cmd
Adapt the following with your source path and place
this in the windows (c:\windows perhaps) path named dms.cmd
```
@dotnet C:\code\dms\src\Documents.Clients.Tools\bin\Debug\netcoreapp2.0\Documents.Clients.Tools.dll %*
```

then 
```
>dms --help
Usage: Documents.Clients.Tools [options] [command]

Options:
  --help                                    Show help information
  --context                                 override the default, saved context and use the mentioned one
  -s|--server <SERVER>                      full url of the DMS API server
  -t|--token <TOKEN>                        specify a token for authentication
  -u|--user-key <USER_KEY>                  specify a UserKey for authentication
  -o|--organization-key <ORGANIZATION_KEY>  specify an OrganizationKey for authentication
  -l|--logging                              enable api client debug logging to console
  --impersonate-organization-key            after authentication, impersonate this organization/user
  --impersonate-user-key                    after authentication, impersonate this organization/user
  -p|--password <PASSWORD>                  spefify a password for authentication
  -n|--no-cached-token                      Do not use auth token cached in context, reauthenticate instead

Commands:
  context
  file
  folder
  import
  index
  log
  notify
  organization                              operations on Organizations
  queue
  search
  security
  server
  sync
  tools
  user

Run 'Documents.Clients.Tools [command] --help' for more information about a command.
```

in your user folder (windows: `c:\users\andy` linux: `/home/andy/`) create a sub-folder
named `.dms`

inside there, place a file `config.json` roughly as follows
```
{
  "DocumentsClientsTools": {
    "CurrentContext": "test",
    "Contexts": {
      "test": {
        "Uri": "https://dmsapi.test.dems/",
        "OrganizationKey": "System",
        "UserKey": "system",
        "Password": "PasswordHere"
      },
      "dev": {
        "Uri": "https://dmsapi.dev.dems/",
        "OrganizationKey": "System",
        "UserKey": "system",
        "Password": "PasswordHere"
      },
      "local": {
        "Uri": "http://localhost:5001/",
        "OrganizationKey": "System",
        "UserKey": "system",
        "Password": "PasswordHere"
      }
    }
  }
}
```

This will represent the different "contexts" that are known to the dms cli.

```
>dms context list
IsCurrent  Key
----------------
*          test
           dev
           local
```

`dms context set dev` will change the default selected context. The active context can also be
changed for a single command
```
>dms --context dev organization get --all
OrganizationKey  Name
------------------------------
System           System
PCMS:259         Training Test
```


# examples

```
C:\code>dms context set prod

C:\code>dms server health
Server                               ms
---------------------------------------------
https://dmsapi.prod.dems.nypti.org/  581.5781


C:\code>dms organization get --all
OrganizationKey  Name
--------------------------------------------------
System           System
PCMS:259         PCMS Training
PCMS:193         PCMS Albany
PCMS:219         PCMS Madison
PCMS:196         PCMS Broome
PCMS:238         PCMS Saratoga
PCMS:235         PCMS Richmond
PCMS:224         PCMS Niagara
PCMS:248         PCMS Ulster
PCMS:228         PCMS Orange
PCMS:237         PCMS St. Lawrence
PCMS:245         PCMS Sullivan
PCMS:249         PCMS Warren
PCMS:200         PCMS Chemung
PCMS:253         PCMS Wyoming
PCMS:204         PCMS Cortland
PCMS:230         PCMS Oswego
PCMS:247         PCMS Tompkins
PCMS:241         PCMS Schuyler
PCMS:254         PCMS Yates
PCMS:199         PCMS Chautauqua
PCMS:227         PCMS Ontario
PCMS:262         Office of Special Narcotics - NYC
PCMS:222         Nassau County
PCMS:233         Queens
PCMS:206         Dutchess


C:\code>dms organization get PCMS:259
OrganizationKey  Name
------------------------------
PCMS:259         PCMS Training


C:\code>dms organization get PCMS:259 --all
OrganizationKey  Name
--------------------------------------------------
System           System
PCMS:259         PCMS Training
PCMS:193         PCMS Albany
PCMS:219         PCMS Madison
PCMS:196         PCMS Broome
PCMS:238         PCMS Saratoga
PCMS:235         PCMS Richmond
PCMS:224         PCMS Niagara
PCMS:248         PCMS Ulster
PCMS:228         PCMS Orange
PCMS:237         PCMS St. Lawrence
PCMS:245         PCMS Sullivan
PCMS:249         PCMS Warren
PCMS:200         PCMS Chemung
PCMS:253         PCMS Wyoming
PCMS:204         PCMS Cortland
PCMS:230         PCMS Oswego
PCMS:247         PCMS Tompkins
PCMS:241         PCMS Schuyler
PCMS:254         PCMS Yates
PCMS:199         PCMS Chautauqua
PCMS:227         PCMS Ontario
PCMS:262         Office of Special Narcotics - NYC
PCMS:222         Nassau County
PCMS:233         Queens
PCMS:206         Dutchess


C:\code>dms organization metadata get PCMS:259
Source        Target        Key                    Value
------------------------------------------------------------------------------------------------------------------------------
organization  organization  type                   "pcms"
organization  organization  synchronize[isactive]  true
organization  organization  searchconfiguration    {\n  "languageMap": {\n    "_path": "Path",\n    "attribute.make": "Camera


C:\code>dms organization metadata get PCMS:259 --all
Source        Target        Key                             Value
---------------------------------------------------------------------------------------------------------------------------------------
organization  organization  type                            "pcms"
organization  organization  synchronize[isactive]           true
organization  organization  searchconfiguration             {\n  "languageMap": {\n    "_path": "Path",\n    "attribute.make": "Camera
organization  folder        ediscovery[isactive]            true
organization  folder        _imagegen[options]              [\n  {\n    "Format": "PNG",\n    "MaxHeight": 100\n  }\n]
organization  folder        _events                         [\n  {\n    "$type": "Documents.API.Common.EventHooks.EventQueueManager, Do
organization  folder        attributelocators[locatorlist]  [\n  {\n    "Key": "_path",\n    "Label": "Path",\n    "IsIndexed": true,\n
organization  folder        leoupload[isactive]             true

C:\code>dms organization metadata get PCMS:259 searchconfiguration
{
  "languageMap": {
    "_path": "Path",
    "attribute.make": "Camera Make",
    "attribute.model": "Camera Model",
    "attribute.casestatus": "Case Status",
    "attribute.closed": "Open or Closed",
    "attribute.closed.true": "Closed",
    "attribute.closed.false": "Open",
    "attribute.ada.last": "Primary ADA",
    "attribute.firstname": "Defendant First name",
    "attribute.lastname": "Defendant Last name"
  },
  "displayFields": [
    "attribute.firstname",
    "attribute.lastname",
    "attribute.casestatus"
  ]
}

C:\code>dms organization metadata get PCMS:259 --all
Source        Target        Key                             Value
---------------------------------------------------------------------------------------------------------------------------------------
organization  organization  type                            "pcms"
organization  organization  synchronize[isactive]           true
organization  organization  searchconfiguration             {\n  "languageMap": {\n    "_path": "Path",\n    "attribute.make": "Camera
organization  folder        ediscovery[isactive]            true
organization  folder        _imagegen[options]              [\n  {\n    "Format": "PNG",\n    "MaxHeight": 100\n  }\n]
organization  folder        _events                         [\n  {\n    "$type": "Documents.API.Common.EventHooks.EventQueueManager, Do
organization  folder        attributelocators[locatorlist]  [\n  {\n    "Key": "_path",\n    "Label": "Path",\n    "IsIndexed": true,\n
organization  folder        leoupload[isactive]             true


C:\code>dms organization metadata get PCMS:259 --all leoupload[isactive]
true

C:\code>dms organization metadata set PCMS:259 leoupload[isactive] --folder true
```


## moving between clusters
`dms --context prod organization metadata get PCMS:259 searchconfiguration | dms --context dev organization metadata set PCMS:259 searchconfiguration`

