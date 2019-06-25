#!/bin/bash
sleep 1
dotnet run -p ../src/Documents.API &
dotnet run -p ../src/Documents.Backends.Gateway &
sleep 5
dotnet run -p ../src/Documents.Clients.Manager &
npm start --prefix ../src/Documents.Clients.Manager/UI &
