#!/bin/bash

executing_dir()
{
        dirname `readlink -f "$0"`
}

exdir=$(executing_dir)

if [ "$1" == "" ]; then
	echo "specify a config file"
	exit 1
fi

cfg="$1"

container=$(cat "$cfg" | jq -r ".docker_containername")
echo "container [$container]"

net=$(cat "$cfg" | jq -r ".docker_networkname")
echo "docker network [$net]"

ip=$(cat "$cfg" | jq -r ".docker_ip")
echo "container ip [$ip]"

urlbase=$(cat "$cfg" | jq -r ".external_url")
echo "urlbase [$urlbase]"

dbfile=$(cat "$cfg" | jq -r ".security_dbfile")
echo "dbfile [$dbfile]"

container_image=searchathing/dotnet:server-mgr
cpus="2"
memory="256m"

echo
echo "---> press a key to continue or ctrl+c to break"
echo

read -n 1

if [ ! -e "$dbfile" ]; then
	echo "  please create dbfile [$dbfile]"
	echo "  see README.md for a simple initial config"
	exit 1
fi

chmod 600 "$dbfile"

echo
echo "---> removing previous container if exists"
echo

docker stop "$container"
docker rm "$container"

docker run \
	-d \
	-ti \
	--name="$container" \
	--network="$net" \
	-e URLBASE="$urlbase" \
	--restart="unless-stopped" \
	-e DOTNET_CLI_TELEMETRY_OPTOUT=1 \
	--volume="$exdir/entrypoint.sh:/entrypoint.d/start.sh" \
	--volume="$exdir/..:/usr/src/securitymanager" \
	--volume="$dbfile:/root/.config/securitymanager/config.json" \
	--cpus="$cpus" \
	--memory="$memory" \
	"$container_image"

docker logs -f "$container"
