#!/bin/bash
# Copyright 2015 Google Inc. All rights reserved.
#
# Licensed under the Apache License, Version 2.0 (the "License");
#you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#    http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and

# /etc/nginx-env/config (stored in K8s Secret 'nginx-env') holds:
#   K8S_SERVICE_FQDN
#   K8S_DNS_HOST
source /etc/documents.network.edge/documents.network.edge

sed -i "s/{{K8S_DNS_HOST}}/${K8S_DNS_HOST}/g;" /etc/nginx/nginx.conf
sed -i "s/{{K8S_SERVICE_FQDN}}/${K8S_SERVICE_FQDN}/g;" /etc/nginx/nginx.conf

cat /etc/nginx/nginx.conf
echo "---------------------"

echo "Starting nginx..."
nginx -g 'daemon off;'