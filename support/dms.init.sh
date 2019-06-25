#!/bin/bash
echo --- shutting down any existing dms processes
sh ./dms.stop.sh

echo --- reinitializing sql container
pushd services/mssql
sh ./reset.sh
popd
echo --- waiting for sql container to become ready
sleep 25

echo --- start dms processes
sh ./dms.start.sh
