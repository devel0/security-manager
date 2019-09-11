#!/bin/bash

source ~/.bashrc

echo "PATH is [$PATH]"

cat ~/.bashrc

#URLBASE="https://sec0.searchathing.com"
#URLBASE="https://sec1.searchathing.com"

replace_token_with()
{
	dotnet /opt/securitymanager/docker/replace-token-with/bin/Debug/netcoreapp2.0/replace-token-with.dll "$@"
}

if [ ! -e /root/.initialized ]; then

	echo
	echo "---> copying distro src to /opt"
	echo
	mkdir -p /opt
	cp -r /usr/src/securitymanager /opt

	echo
	echo "---> building replace-token-with utility"
	echo
	dotnet build /opt/securitymanager/docker/replace-token-with

	echo
	echo "---> setup webapi url to [$URLBASE]"
	echo
	appjs=/opt/securitymanager/SecurityManagerClient/app.js
	cat "$appjs" | replace_token_with "urlbase = 'http://localhost:5000'" "urlbase = '$URLBASE'" | replace_token_with "debugmode = true;" "debugmode = false;" > /tmp/x && mv -f /tmp/x "$appjs"

	echo
	echo "---> building web api server"
	echo
	cd /opt/securitymanager/SecurityManagerWebapi
	rm -fr obj bin
	dotnet restore .
	dotnet build -c Release .

	echo
	echo "---> building web client"
	echo
	cd /opt/securitymanager/SecurityManagerClient
	bower install --allow-root

	touch /root/.initialized
fi

echo
echo "---> run webapi server"
echo
dotnet /opt/securitymanager/SecurityManagerWebapi/bin/Release/netcoreapp2.0/SecurityManagerWebapi.dll &

echo
echo "---> run web server"
echo
cd /opt/securitymanager/SecurityManagerClient
ws -p 80 --spa index.html &

echo
echo "===> app ready ( ctrl+c to stop log )"
echo
