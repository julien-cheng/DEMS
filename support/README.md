# Requirements and Dependencies
Install Docker
- Docker - https://docs.docker.com/docker-for-windows/install/
- docker-compose - https://docs.docker.com/compose/install/

The [services folder](services) provides a near complete set of service containers needed to run the system
- Redis
- Elastic/Kibana
- MSSQLServer
- RabbitMQ
- unoconv

then you can run scripts
- [`up.cmd`](services/up.cmd) starts all the containers
- [`down.cmd`](services/down.cmd) stops all the containers
- [`reset.cmd`](services/reset.cmd) zeros the persistent storage for mssql and restarts it


# Create the config environment
An example server-side config file can be found at [config/documents.server.config.config.json](config/documents.server.config.json). To locate this file, 
an environment variable must be supplied with the full path to the file:
`DOCUMENTS_CONFIG_PATH=c:\code\documents\support\config\documents.server.config.json`

Be sure to adjust the path above to match your code location

Separately, the `dms` CLI tool looks for a config.json to be located under your user profile directory. If you already have one, merge in the `local`
context from the sample config file into your existing file.
```
mkdir c:\users\loginname\.dms
copy support\config\config.json c:\users\loginname\.dms\config.json
```

# Install the dms CLI tool

Copy the batch file into your search path (to use the c:\windows directory you will need to launch an Administrative Command Prompt)
Next, *edit the file* to set the correct path to the build location of the Documents.Clients.Tools output folder!

```
copy support\scripts\dms.cmd c:\windows
notepad c:\windows\dms.cmd
```

Confirm that the dms CLI tool is working
```
dms --help
```

# Build the system
## Visual Studio
Open Documents.sln, Ctrl-Shift-B

## VS Code
Open the root folder (install any recommended extensions), Ctrl-Shift-B

## CLI
```dotnet build```

# Setup demo environment
create an empty path `c:\dms\store`

then run `dms.init.basic.cmd`

and if all goes well, it will start Documents.API, Documents.Gateway, Documents.Clients.Manager, and the Documents.Clients.Manager/UI ng proxy then it provisions a new basic organization with the File System backend drivers pointed to c:\dms\store

finally, it launches browser instance to http://localhost:4200/JWTAuth/Backdoor and logs into the new organization

# Restart demo
Running `dms.init.basic.cmd` will kill all previous processes, zero the database, and restart the system as if from a fresh state.