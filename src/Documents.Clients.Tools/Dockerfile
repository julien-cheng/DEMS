FROM mcr.microsoft.com/dotnet/core/runtime:2.2 AS runtime
WORKDIR /app
COPY --from=documents:localbuild /app/src/Documents.Clients.Tools/bin/Release/netcoreapp2.0/publish/ /app

RUN chmod +x /app/dms.sh
RUN ln -s /app/dms.sh /bin/dms
CMD exec /bin/bash -c "trap : TERM INT; sleep infinity & wait"
