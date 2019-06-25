@echo off
echo --- shutting down any existing dms processes
call dms.stop.cmd

echo --- reinitializing sql container
pushd services\mssql
call reset.cmd
popd
echo --- waiting for sql container to become ready
timeout 25

echo --- start dms processes
call dms.start.cmd

