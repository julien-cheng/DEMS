@echo off
call dms.init.cmd

echo --- waiting dms api to be ready
timeout 25

echo --- clearing out file store
del /q c:\dms\store\*.*
echo --- provisioning demo organization with file store
call dms --context local organization provision basic --name demo --organizationKey demo --basepath c:\dms\store

echo --- creating demo user
call dms --context local user create --email user.one@somedomain.com --password password --access-identifiers o:demo demo/user1

start http://localhost:4200/JWTAuth/Backdoor
