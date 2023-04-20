#!/bin/bash

source ~/.bashrc

echo "PATH is [$PATH]"

#cat ~/.bashrc

#URLBASE="https://sec0.searchathing.com"
#URLBASE="https://sec1.searchathing.com"

echo
echo "---> run webapi server"
echo
dotnet /opt/securitymanager/SecurityManagerWebapi/bin/Release/net7.0/SecurityManagerWebapi.dll &

echo
echo "---> run web server"
echo
cd /opt/securitymanager/SecurityManagerClient

ws -p 80 --spa index.html &

echo
echo "===> app ready ( ctrl+c to stop log )"
echo

$*