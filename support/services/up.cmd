pushd mssql
docker-compose up -d
popd

pushd rabbit
docker-compose up -d
popd

pushd elk
docker-compose up -d
popd

pushd unoconv
docker-compose up -d
popd

pushd redis
docker-compose up -d
popd
