FROM mcr.microsoft.com/dotnet/core/runtime:2.2 AS runtime
WORKDIR /app
COPY --from=documents:localbuild /app/src/Documents.API/bin/Release/netcoreapp2.0/publish/ /app
ENTRYPOINT ["dotnet", "Documents.API.dll"]
EXPOSE 5001