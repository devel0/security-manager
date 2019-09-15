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

mkdir -p "$exdir"/src-copy
rsync -arvx --delete --exclude=docker "$exdir"/../ "$exdir"/src-copy/

echo "# DO NOT EDIT THIS = AUTOMATICALLY GENERATED" > "$exdir"/Dockerfile
cat "$exdir"/Dockerfile.top >> "$exdir"/Dockerfile
echo "ENV URLBASE=$urlbase" >> "$exdir"/Dockerfile
cat "$exdir"/Dockerfile.bottom >> "$exdir"/Dockerfile

docker build -t $container -f "$exdir"/Dockerfile "$exdir"/.

echo
echo "---> removing previous container if exists"
echo

docker stop "$container"
docker rm "$container"

docker run -d \
         --name="$container" \
         -h "$container" \
         --cpus="$cpus" \
         --memory="$memory" \
         --restart=unless-stopped \
	-ti \
	--network="$net" \
	--volume="$exdir/entrypoint.sh:/entrypoint.d/start.sh" \
	--volume="$dbfile:/root/.config/securitymanager/config.json" \
	"$container"

docker logs -f "$container"
