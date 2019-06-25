FROM mcr.microsoft.com/dotnet/core/runtime:2.2 AS runtime
WORKDIR /app
COPY --from=documents:localbuild /app/src/Documents.Backends.Gateway/bin/Release/netcoreapp2.0/publish/ /app
ENTRYPOINT ["dotnet", "Documents.Backends.Gateway.dll"]

EXPOSE 5020
HEALTHCHECK CMD curl --fail http://localhost:5020/healthcheck || exit 1
