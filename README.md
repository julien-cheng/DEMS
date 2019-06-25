[![NYPTI Logo](https://www.nypti.org/wp-content/uploads/2014/01/NYPTISeal-170.png)](https://www.nypti.org/)

# NYPTI's Digital Evidence Management System (DEMS)

DEMS is a digital evidence file management system from the [New York Prosecutors Training Institute](https://www.nypti.org) designed for today's prosecutors to help them meet Discovery obligations.  

Read about DEMS and it's features [here.](https://books.nypti.org/bedu/dcty/index.html)

Additional information coming in the near future. 

# Documents

## Requirements
* Install [Docker for Windows](https://www.docker.com/docker-windows)
* Install [Node.js](https://www.nodejs.org/)

## First Run
1. Download project
2. `\> build.cmd`
3. `\support\compose-full> up.cmd`
4. `\support> init.cmd`
5. In a new shell `\src\Documents.Clients.Manager\UI> npm i` 
6. `\src\Documents.Clients.Manager\UI> npm run start` 
7. Open https://localhost:4200/JWTAuth/Backdoor

A database will be created at first use and initialized with
an organization named `System`, user `system` (note case)

That user will have privileges to create other organizations
and define their security models. see: Documents.Provisioning

### Creating dms.cmd
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

## Development
We recommend [Visual Studio Code](https://code.visualstudio.com/download/)

Create system environment variable named `DOCUMENTS_CONFIG_PATH` pointing
to the config.json (non-public-state/contains-secrets). For example, `export DOCUMENTS_CONFIG_PATH=/Users/you/Documents/support/config/documents.server.config.json`

Open Documents in VSCode

Once you have changed a few files run:
1. Run `docker build -t documents:localbuild .` to copy the files into the docker containers
2. Run `docker build -t documents.api src/Documents.API` or equivalent to rebuild your container
3. Restart the container from the VSCode [Docker extension](https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-docker)

The core projects:
* Documents.API - [link](http://localhost:5001/api/v1/health)
* Documents.Backends.Gateway - [link](http://localhost:5020/healthcheck)
* Documents.Backends.Manager/UI - (needs ng client: see ng.cmd) [link](http://localhost:4200/JWTAuth/Backdoor)
* Documents.Queues.Tasks.EventRouter
* Documents.Queues.Tasks.Index

### Containers
```
IMAGE                                                     PORTS
documents.clients.manager:latest                          0.0.0.0:5000->5000/tcp, 5001/tcp
documents.queues.tasks.pdfocr:latest                      
documents.queues.tasks.archive:latest                     
documents.queues.tasks.imagegen:latest                    
documents.queues.tasks.eventrouter:latest                 
documents.queues.tasks.transcode.ffmpeg:latest            
documents.queues.tasks.textextract:latest                 
documents.queues.tasks.exiftool:latest                    
documents.api:latest                                      0.0.0.0:5001->5001/tcp
redis:alpine                                              6379/tcp
documents.backends.gateway:latest                         5020/tcp
rabbitmq:3.7.4-management-alpine                          4369/tcp, 5671-5672/tcp, 15671/tcp, 25672/tcp, 0.0.0.0:15672->15672/tcp
docker.elastic.co/elasticsearch/elasticsearch-oss:6.6.1   9200/tcp, 9300/tcp
mcr.microsoft.com/mssql/server:latest                     0.0.0.0:1433->1433/tcp
```

These continers expose TCP ports:
* 15672 - [RabbitMQ Web-based admin interface](http://localhost:15672) [login guest/guest]
* 5672 - RabbitMQ protocol
* 1433 - Microsoft SQL Server Express
* 3000 - Unoconv (http service that uses open office to convert documents to PDF)
* 5601 - Kibana connected to elastic
* 9200 - Elastic Search Service
