#!/bin/bash

registry=359927049497.dkr.ecr.us-east-1.amazonaws.com/dems
tag=$1

$(aws ecr get-login --no-include-email)


pushimage documents:localbuild
pushimage documents.queues.tasks.logreader
pushimage documents.queues.tasks.index
pushimage documents.clients.manager
pushimage documents.clients.admin
pushimage documents.queues.tasks.voicebase
pushimage documents.queues.tasks.transcode.ffmpeg
pushimage documents.queues.tasks.topdf
pushimage documents.queues.tasks.textextract
pushimage documents.queues.tasks.synchronize
pushimage documents.queues.tasks.pdfocr
pushimage documents.queues.tasks.notify
pushimage documents.queues.tasks.imagegen
pushimage documents.queues.tasks.exiftool
pushimage documents.queues.tasks.eventrouter
pushimage documents.queues.tasks.archive
pushimage documents.clients.tools
pushimage documents.clients.pcmsbridge
pushimage documents.backends.gateway
pushimage documents.api
pushimage documents.network.edge
pushimage documents.network.bastion
exit



function pushimage()
{
    docker tag $1:latest $registry/$1:$tag
    docker push $registry/$1:$tag

}

