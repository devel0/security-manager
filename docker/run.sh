#!/bin/bash

if [ "$(which jq)" == "" ]; then
	echo "please install jq"
	exit 1
fi

executing_dir()
{
        dirname `readlink -f "$0"`
}

exdir=$(executing_dir)

if [ "$1" == "" ]; then
	echo "specify a config file"
	exit 1
fi

if [ ! -e "$1" ]; then
	echo "[$1] conf file not exists"
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
echo "$urlbase" > "$exdir"/src-copy/urlbase

docker build -t $container -f "$exdir"/Dockerfile "$exdir"/.

if [ "$?" != 0 ]; then
	echo "fail build image"
	exit 1
fi

echo
echo "---> removing previous container if exists"
echo

q=$(docker ps -a --format "{{.Names}}" | grep ^$container$)
if [ "$q" != "" ]; then
	docker stop "$container"
	docker rm "$container"
fi

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
