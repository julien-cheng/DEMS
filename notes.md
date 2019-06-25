clone repo


```
cd src/Documents.API
dotnet run
```

```
cd src/Documents.Backends.Gateway
dotnet run
```

```
cd src/Documents.Clients.Manager/UI
npm i
npm start
```

~/.dms/config.json
```
{
  "CurrentContext": "local",
  "Contexts": {
    "local": {
      "Uri": "http://localhost:5001/",
      "OrganizationKey": "System",
      "UserKey": "system",
      "Password": "DocumentsDefault"
    }
  }
}
```

```
dms organization provision basic --name demo --organizationKey demo --basepath c:\dms\store
OrganizationKey  Name
---------------------
demo             demo
```

```
dms --no-cached-token user set System/system --password DocumentsDefault
```

```
dms organization provision basic --name demo --organizationKey demo --basepath c:\dms\store
OrganizationKey  Name
---------------------
demo             demo
```

```
dms user create --email user.one@somedomain.com --password password --access-identifiers o:demo demo/user1
OrganizationKey  UserKey  EmailAddress             UserAccessIdentifiers
------------------------------------------------------------------------
demo             user1    user.one@somedomain.com

```

```
dms --no-cached-token --organization-key demo --user-key user1 --password password organization get demo
OrganizationKey  Name
---------------------
demo             demo
```

```
{
  "DocumentsAPI": {
    "ElasticSearchUri": "http://localhost:9200",
    "ElasticSearchIndex": "documents",
    "QueueURI": "amqp://guest:guest@localhost:5672",
    "ConnectionString": "Server=localhost; Database=Documents; user id=sa; password=wwFRtn9aCa3kAv9J",
    "BackendGatewayURL": "http://localhost:5020/",
    "TokenValidationSecret": "SooperSecre1231234t!",
    "TokenIssuer": "API",
    "TokenAudience": "API",
    "TokenExpirationSeconds": 90000
  },
  "DocumentsQueuesTasksIndex": {
    "API": {
      "Uri": "http://localhost:5001/",
      "OrganizationKey": "System",
      "UserKey": "system",
      "Password": "DocumentsDefault"
    },
    "ElasticSearchUri": "http://localhost:9200",
    "StartupDelay": 4000
  },
  "DocumentsQueuesTasksEventRouter": {
    "API": {
      "Uri": "http://localhost:5001/",
      "OrganizationKey": "System",
      "UserKey": "system",
      "Password": "DocumentsDefault"
    }
  },
  "DocumentsQueuesTasksToPDF": {
    "API": {
      "Uri": "http://localhost:5001/",
      "OrganizationKey": "System",
      "UserKey": "system",
      "Password": "DocumentsDefault"
    },
    "UnoConvUri": "http://localhost:3000/proxy/unoconv/pdf"
  },
  "DocumentsQueuesTasksImageGen": {
    "API": {
      "Uri": "http://localhost:5001/",
      "OrganizationKey": "System",
      "UserKey": "system",
      "Password": "DocumentsDefault"
    }
  },
  "DocumentsQueuesTasksArchive": {
    "API": {
      "Uri": "http://localhost:5001/",
      "OrganizationKey": "System",
      "UserKey": "system",
      "Password": "DocumentsDefault"
    }
  },
  "DocumentsQueuesTasksTextExtract": {
    "API": {
      "Uri": "http://localhost:5001/",
      "OrganizationKey": "System",
      "UserKey": "system",
      "Password": "DocumentsDefault"
    }
  },
  "DocumentsClientsManager": {
    "API": {
      "Uri": "http://localhost:5001/"
    },
    "IsBackdoorEnabled": true,
    "BackdoorOrganizationKey": "demo",
    "BackdoorUserKey": "user1",
    "BackdoorPassword": "password"
  },

  "DocumentsQueuesTasksTranscodeFFMPEG": {
    "API": {
      "Uri": "http://localhost:5001/",
      "OrganizationKey": "System",
      "UserKey": "system",
      "Password": "DocumentsDefault"
    }
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Warning",
      "Override": {
        "Documents": "Information"
      }
    },
    "WriteTo": [
      {
        "Name": "LiterateConsole",
        "Args": {
          "outputTemplate": "{Timestamp:mm:ss.fff}-{Level} {SourceContext} {Message}{NewLine}{Exception}"
        }
      },
      {
        "Name": "Elasticsearch",
        "Args": {
          "nodeUris": "http://localhost:9200",
          "indexFormat": "documents-logging"
        }
      }
    ]
  }
}

```