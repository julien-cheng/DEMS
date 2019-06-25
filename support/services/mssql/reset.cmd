docker-compose down
del /q ..\data\mssql\*.*
docker-compose up -d
