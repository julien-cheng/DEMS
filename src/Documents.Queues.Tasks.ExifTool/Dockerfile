FROM mcr.microsoft.com/dotnet/core/runtime:2.2 AS runtime
RUN apt-get update && apt-get install -y libimage-exiftool-perl
WORKDIR /app
COPY --from=documents:localbuild /app/src/Documents.Queues.Tasks.ExifTool/bin/Release/netcoreapp2.0/publish/ /app
ENTRYPOINT ["dotnet", "Documents.Queues.Tasks.ExifTool.dll"]