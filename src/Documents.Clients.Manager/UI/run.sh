#!/bin/bash
npm update
npm install
ng serve --proxy-config proxy.conf.andy.json --host 0.0.0.0 --verbose true

