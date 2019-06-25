#!/bin/bash

docker build -t documents:localbuild .
docker build -t documents.api src/Documents.API
docker build -t documents.backends.gateway src/Documents.Backends.Gateway
docker build -t documents.clients.admin src/Documents.Clients.Admin
docker build -t documents.clients.manager src/Documents.Clients.Manager
docker build -t documents.clients.pcmsbridge src/Documents.Clients.PCMSBridge
docker build -t documents.clients.tools src/Documents.Clients.Tools
docker build -t documents.clients.pcmsbridge src/Documents.Clients.PCMSBridge
docker build -t documents.network.bastion src/Documents.Network.Bastion
docker build -t documents.network.edge src/Documents.Network.Edge

docker build -t documents.queues.tasks.archive src/Documents.Queues.Tasks.Archive
docker build -t documents.queues.tasks.eventrouter src/Documents.Queues.Tasks.EventRouter
docker build -t documents.queues.tasks.exiftool src/Documents.Queues.Tasks.ExifTool
docker build -t documents.queues.tasks.imagegen src/Documents.Queues.Tasks.ImageGen
docker build -t documents.queues.tasks.index src/Documents.Queues.Tasks.Index
docker build -t documents.queues.tasks.logreader src/Documents.Queues.Tasks.LogReader
docker build -t documents.queues.tasks.notify src/Documents.Queues.Tasks.Notify
docker build -t documents.queues.tasks.pdfocr src/Documents.Queues.Tasks.PDFOCR

docker build -t documents.queues.tasks.synchronize src/Documents.Queues.Tasks.Synchronize
docker build -t documents.queues.tasks.textextract src/Documents.Queues.Tasks.TextExtract
docker build -t documents.queues.tasks.topdf src/Documents.Queues.Tasks.ToPDF
docker build -t documents.queues.tasks.transcode.ffmpeg src/Documents.Queues.Tasks.Transcode.FFMPEG
docker build -t documents.queues.tasks.voicebase src/Documents.Queues.Tasks.Voicebase
