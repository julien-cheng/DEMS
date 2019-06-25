[![NYPTI Logo](https://www.nypti.org/wp-content/uploads/2014/01/NYPTISeal-170.png)](https://www.nypti.org/)

# NYPTI's Digital Evidence Management System (DEMS)

DEMS is a digital evidence file management system from the [New York Prosecutors Training Institute](https://www.nypti.org) designed for today's prosecutors to help them meet Discovery obligations.  

Read about DEMS and it's features [here.](https://books.nypti.org/bedu/dcty/index.html)

Additional information coming in the near future. 

# Documents

## Development
### Windows
* Install [Docker for Windows](https://www.docker.com/docker-windows)
* Install [Visual Studio Community](https://www.visualstudio.com/downloads/) update 15.5 or later

Start docker services

```
\support\services> up.cmd
```

this should start 5 containers

```
CONTAINER ID        IMAGE                                 COMMAND                  CREATED             STATUS              PORTS                                                                                        NAMES
b0aa6d0aa55b        rabbitmq:3.7.4-management-alpine      "docker-entrypoint.s…"   31 hours ago        Up 31 hours         4369/tcp, 5671/tcp, 0.0.0.0:5672->5672/tcp, 15671/tcp, 25672/tcp, 0.0.0.0:15672->15672/tcp   rabbit_rabbitmq_1
f60448dada40        microsoft/mssql-server-linux:latest   "/bin/sh -c /opt/mss…"   7 weeks ago         Up 38 hours         0.0.0.0:1433->1433/tcp                                                                       mssql_mssql-server-linux_1
465c4964740e        zrrrzzt/docker-unoconv-webservice     "/bin/sh -c '/usr/bi…"   7 weeks ago         Up 32 hours         0.0.0.0:3000->3000/tcp                                                                       unoconv_unoconv_1
00567759d55f        elk_kibana                            "/bin/bash /usr/loca…"   3 months ago        Up 38 hours         0.0.0.0:5601->5601/tcp                                                                       elk_kibana_1
d7bb329a5f55        elk_elasticsearch                     "/usr/local/bin/dock…"   3 months ago        Up 38 hours         0.0.0.0:9200->9200/tcp, 0.0.0.0:9300->9300/tcp                                               elk_elasticsearch_1
```

This will open TCP ports 

* 15672 - [RabbitMQ Web-based admin interface](http://localhost:15672) [login guest/guest]
* 5672 - RabbitMQ protocol
* 1433 - Microsoft SQL Server Express
* 3000 - Unoconv (http service that uses open office to convert documents to PDF)
* 5601 - Kibana connected to elastic
* 9200 - Elastic Search Service

Create system environment variable named `DOCUMENTS_CONFIG_PATH` pointing
to the config.json (non-public-state/contains-secrets).

Open Documents.sln in visual studio

Projects to consider adding to your Solution Startup Projects
* Documents.API - [link](http://localhost:5001/api/v1/health)
* Documents.Backends.Gateway - [link](http://localhost:5020/healthcheck)
* Documents.Backends.Manager - (needs ng client: see ng.cmd) [link](http://localhost:4200/JWTAuth/Backdoor)
* Documents.Queues.Tasks.EventRouter
* Documents.Queues.Tasks.Index

The database will be created at first use and initialized with
an organization named `System`, user `system` (note case)

That user will have privileges to create other organizations
and define their security models. see: Documents.Provisioning

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
  --context                                 UseContext
  -s|--server <SERVER>                      Server
  -t|--token <TOKEN>                        Token
  -u|--user-key <USER_KEY>                  UserKey
  -o|--organization-key <ORGANIZATION_KEY>  OrganizationKey
  --impersonate-organization-key            ImpersonateOrganizationKey
  --impersonate-user-key                    ImpersonateUserKey
  -p|--password <PASSWORD>                  Password
  -n|--no-cached-token                      Do not use auth token cached in context, reauthenticate instead

Commands:
  context
  file
  folder
  index
  organization
  search
  security
  server
  sync
  user

Run 'Documents.Clients.Tools [command] --help' for more information about a command.


```