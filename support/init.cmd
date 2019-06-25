call dms --context local organization provision basic --name demo --organizationKey demo --basepath c:\code\dms\support\services\data\store
call dms --context local user create --email user.one@somedomain.com --password password --access-identifiers "o:demo x:eDiscovery" demo/user1
REM call dms --context local organization metadata set demo --folder _imagegen[options] --from-file imagegen.org.folder.json

call dms --context local organization metadata set --folder demo ediscovery[isactive] true
call dms --context local organization metadata set --folder demo leoupload[isactive] true
