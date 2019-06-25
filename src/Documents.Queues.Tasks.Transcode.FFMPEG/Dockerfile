FROM mcr.microsoft.com/dotnet/core/runtime:2.2.3-alpine3.8 AS runtime

RUN apk add --no-cache --update libgcc libstdc++ ca-certificates libcrypto1.0 libssl1.0 libgomp expat git

COPY --from=jrottenberg/ffmpeg:4.1-alpine /usr/local /usr/local/


WORKDIR /app
COPY --from=documents:localbuild /app/src/Documents.Queues.Tasks.Transcode.FFMPEG/bin/Release/netcoreapp2.0/publish/ /app
ENTRYPOINT ["dotnet", "Documents.Queues.Tasks.Transcode.FFMPEG.dll"]
