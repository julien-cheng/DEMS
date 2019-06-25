#!/bin/bash
pushd mssql
docker-compose down
popd

pushd rabbit
docker-compose down
popd

pushd elk
docker-compose down
popd

pushd unoconv
docker-compose down
popd

