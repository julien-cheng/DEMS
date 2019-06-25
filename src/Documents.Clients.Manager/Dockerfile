FROM node:8 AS build
WORKDIR /app
COPY --from=documents:localbuild /app/src/Documents.Clients.Manager/UI/package*.json /app/
RUN npm install
COPY --from=documents:localbuild /app/src/Documents.Clients.Manager/UI/ /app
RUN npm run build-prod-server

FROM mcr.microsoft.com/dotnet/core/runtime:2.2 AS runtime
WORKDIR /app
COPY --from=build /app/dist /app/wwwroot
COPY --from=documents:localbuild /app/src/Documents.Clients.Manager/bin/Release/netcoreapp2.0/publish/ /app
EXPOSE 5001
ENTRYPOINT ["dotnet", "Documents.Clients.Manager.dll"]
HEALTHCHECK CMD curl --fail http://localhost:5000/healthcheck || exit 1