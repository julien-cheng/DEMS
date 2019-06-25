#!/bin/bash
DIR=`dirname $(readlink -f $0)`
dotnet $DIR/Documents.Clients.Tools.dll "$@"
