#!/bin/bash

bash ./dms.init.sh

echo --- waiting dms api to be ready

sleep 25

echo --- clearing out file store
rm /dms/store/*
echo --- provisioning demo organization with file store
./scripts/dms.sh --context local organization provision basic --name demo --organizationKey demo --basepath /dms/store

echo --- creating demo user
./scripts/dms.sh --context local user create --email user.one@somedomain.com --password password --access-identifiers o:demo demo/user1

open http://localhost:4200/JWTAuth/Backdoor
