# security manager

Webapi + webapp personal wallet cloud 

## motivations and architecture overview

- don't want your password stored into external cloud
  - have your own server with
    - nginx https ( [letsencrypt](https://letsencrypt.org/) ) valid certificate
    - docker container in separate dedicated network space (/30) with custom firewall rules
- use of firefox browser ( browser password stored local and synced between your device, not device-cloud ; must backup your data )
- on your palm device access through browser https to the wallet with
  - password ( real random ) to protect against internet face ( browser can store that password )
  - pin ( 4 numbers ) to protect against wallet use ( browser can't store that pin )

## prerequisites

- build [dotnet bionic](https://github.com/devel0/docker-dotnet/blob/bionic/README.md)

## setup

- create a config json file with follow initial structure

```json
{
  "AdminPassword": "supersecret",
  "Pin": nnnn
}
```

replacing
- `supersecret` with a cleartext real random password
- `nnnn` with a 4 number pin

chmod that file 600 otherwise application generate an error

- increase security : encrypt your hard disk to avoid, if stolen, data can be read

- create a docker network

```
docker network create sec0 --subnet=10.10.0.56/30
```

- adjust the sample [config-sec0.json](docker/config-sec0.json) with your own `security_dbfile` place and `external_url` that is the real url
- create autorestarting container

```
cd ./docker
./run config-sec0.json
```

## debug

- prerequisites
  - [nodejs, local web server](https://github.com/devel0/docker-ubuntu/blob/db474a1a65638d42351bbefe318ffc47736b820b/Dockerfile#L21-L26)
  - vscode ( suggested [extensions](https://github.com/devel0/knowledge/blob/master/tools/vscode-useful-extensions.md) )
  - firefox ( debug [settings](https://github.com/devel0/knowledge/blob/master/webdevel/vscode-debug-firefox.md) )

- vscode and start **webapi server**

```
cd ./SecurityManagerWebapi
code . &
```

and hit F5 `.NET Core Launch (console)` after Omnisharp initializes

- close firefox and start firefox debug using `firefox --start-debugger-server` ( may useful to disable breakpoint on unhandled exception from F12 to avoid occasional thirdy pages browsing breakpoint )

- vscode and start **web server**

```
cd ./SecurityManagerClient
code . &
ws -p 80 --spa index.html
```

and hit F5 `Launch localhost`

- browse http://localhost from opened firefox debug window

- notes:
  - breakpoint will work on webapi and client javascript
