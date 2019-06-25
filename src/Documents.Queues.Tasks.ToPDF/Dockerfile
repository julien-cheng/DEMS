FROM mcr.microsoft.com/dotnet/core/runtime:2.2 AS runtime
WORKDIR /app
COPY --from=documents:localbuild /app/src/Documents.Queues.Tasks.ToPDF/bin/Release/netcoreapp2.0/publish/ /app
ENTRYPOINT ["dotnet", "Documents.Queues.Tasks.ToPDF.dll"]
