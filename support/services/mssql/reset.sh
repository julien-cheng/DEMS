#!/bin/bash

docker-compose down
rm ../data/mssql/*
docker-compose up -d
