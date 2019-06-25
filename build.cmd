docker build -t documents:localbuild .
call :build documents.api Documents.API
call :build documents.backends.gateway Documents.Backends.Gateway
call :build documents.clients.admin Documents.Clients.Admin
call :build documents.clients.manager Documents.Clients.Manager
call :build documents.clients.pcmsbridge Documents.Clients.PCMSBridge
call :build documents.clients.tools Documents.Clients.Tools
call :build documents.clients.pcmsbridge Documents.Clients.PCMSBridge
call :build documents.network.bastion Documents.Network.Bastion
call :build documents.network.edge Documents.Network.Edge

call :build documents.queues.tasks.archive Documents.Queues.Tasks.Archive
call :build documents.queues.tasks.eventrouter Documents.Queues.Tasks.EventRouter
call :build documents.queues.tasks.exiftool Documents.Queues.Tasks.ExifTool
call :build documents.queues.tasks.imagegen Documents.Queues.Tasks.ImageGen
call :build documents.queues.tasks.index Documents.Queues.Tasks.Index
call :build documents.queues.tasks.logreader Documents.Queues.Tasks.LogReader
call :build documents.queues.tasks.notify Documents.Queues.Tasks.Notify
call :build documents.queues.tasks.pdfocr Documents.Queues.Tasks.PDFOCR

call :build documents.queues.tasks.synchronize Documents.Queues.Tasks.Synchronize
call :build documents.queues.tasks.textextract Documents.Queues.Tasks.TextExtract
call :build documents.queues.tasks.topdf Documents.Queues.Tasks.ToPDF
call :build documents.queues.tasks.transcode.ffmpeg Documents.Queues.Tasks.Transcode.FFMPEG
call :build documents.queues.tasks.voicebase Documents.Queues.Tasks.Voicebase

goto :EOF


:build
docker build -t %1 src/%2
if ERRORLEVEL 2 goto :error
exit /b 0

:error
echo Failed with error #%errorlevel%.
exit /b %errorlevel%