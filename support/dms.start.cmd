call dms.stop.cmd
timeout 1
start "Documents.API" /D ..\src\Documents.API dotnet run
start "Documents.Backends.Gateway" /D ..\src\Documents.Backends.Gateway dotnet run
timeout 5
start "Documents.Clients.Manager" /D ..\src\Documents.Clients.Manager dotnet run
start "Documents.Clients.Manager-UI" /D ..\src\Documents.Clients.Manager\UI npm start
