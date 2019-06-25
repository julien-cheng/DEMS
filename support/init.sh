#!/bin/bash
../scripts/dms.sh --context local organization provision basic --name demo --organizationKey demo --basepath /mnt/store
../scripts/dms.sh --context local user create --email user.one@somedomain.com --password password --access-identifiers "o:demo x:eDiscovery" demo/user1
../scripts/dms.sh --context local organization metadata set demo --folder _imagegen[options] --from-file imagegen.org.folder.json
