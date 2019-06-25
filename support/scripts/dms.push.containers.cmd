@echo off

set registry=359927049497.dkr.ecr.us-east-1.amazonaws.com/dems
set tag=%1

call :aws

docker tag documents:localbuild %registry%/documents:%tag%

call :doimage documents.queues.tasks.logreader
call :doimage documents.queues.tasks.index
call :doimage documents.clients.manager
call :doimage documents.clients.admin
call :doimage documents.queues.tasks.voicebase
call :doimage documents.queues.tasks.transcode.ffmpeg
call :doimage documents.queues.tasks.topdf
call :doimage documents.queues.tasks.textextract
call :doimage documents.queues.tasks.synchronize
call :doimage documents.queues.tasks.pdfocr
call :doimage documents.queues.tasks.notify
call :doimage documents.queues.tasks.imagegen
call :doimage documents.queues.tasks.exiftool
call :doimage documents.queues.tasks.eventrouter
call :doimage documents.queues.tasks.archive
call :doimage documents.clients.tools
call :doimage documents.clients.pcmsbridge
call :doimage documents.backends.gateway
call :doimage documents.api
call :doimage documents.network.edge
call :doimage documents.network.bastion
exit /b 0

:doimage
docker tag %1:latest %registry%/%1:%tag%
if %ERRORLEVEL% == 1 goto :fail
docker push %registry%/%1:%tag%
if %ERRORLEVEL% == 1 goto :fail
exit /b 0

:aws
echo Attempting to loging to Amazon ECR
for /f "tokens=*" %%i in ('aws ecr get-login --no-include-email') do set LOGIN=%%i
cmd /c %LOGIN%
exit /b 0

:fail
echo push failed. sorry.