FROM mcr.microsoft.com/dotnet/core/runtime:2.2 AS runtime
WORKDIR /app
COPY --from=documents:localbuild /app/src/Documents.Clients.PCMSBridge/bin/Release/netcoreapp2.0/publish/ /app
EXPOSE 5050
ENTRYPOINT ["dotnet", "Documents.Clients.PCMSBridge.dll"]
