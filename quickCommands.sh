#!/bin/bash

# ----------------------------------------------
# THIS FILE IS NOT INTENEDED TO BE RUN
# This allows you to run all pieces individually
# ----------------------------------------------


echo DO NOT RUN THIS FILE

DIRECTORY=`pwd`
export DOCUMENTS_CONFIG_PATH=$DIRECTORY/support/config/documents.server.config.json

cd support/compose-debug
docker-compose up -d
cd ../../

cd src/Documents.Backends.Gateway
dotnet run &
cd ../../

cd src/Documents.Clients.Manager
dotnet run &
cd ../../

cd src/Documents.Api
dotnet run &
cd ../../

cd src/Documents.Queues.Tasks.EventRouter
dotnet run &
cd ../../

cd src/Documents.Queues.Tasks.Index
dotnet run &
cd ../../

sleep 10

cd support/compose-full
./init.sh
cd ../../

cd src/Documents.Clients.Manager/UI
npm run start &
cd ../../../

# --------------------------
# Alternatively, you can run
# --------------------------

cd support/compose-debug
docker-compose up -d
sleep 10
./init.sh
cd ../../

cd src/Documents.Clients.Manager/UI
npm run start &
cd ../../../
