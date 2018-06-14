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
  - vscode ( suggested [extensions](https://github.com/devel0/knowledge/blob/daea0a3439467e882326ecc3a9e5fbd7d7b17441/tools/vscode-useful-extensions.md) )
  - firefox ( debug [settings](https://github.com/devel0/knowledge/blob/daea0a3439467e882326ecc3a9e5fbd7d7b17441/webdevel/vscode-debug-firefox.md) )

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

## code map

### webapi ( server )

|**section**|**description**|
|---|---|
| [Global](SecurityManagerWebapi/Global.cs) | path names and logging functions |
| [LinuxHelper](SecurityManagerWebapi/LinuxHelper.cs) | unix syscall mapping through Mono.Posix.NETStandard net core library |
| Startup,Program | standard from `dotnet new webapi` |
| [Types/Common](SecurityManagerWebapi/Types/Common.cs) | common json request data structure |
| [Types/CommonResponse](SecurityManagerWebapi/Types/CommonResponse.cs) | common json response data structure |
| [Types/Config](SecurityManagerWebapi/Types/Config.cs) | dbfile structure - will json (de)serialized |
| [Types/CredInfo](SecurityManagerWebapi/Types/CredInfo.cs) | base dbfile record type - credential record and json file locking add/remove record, save |
| [Controllers/ApiController](SecurityManagerWebapi/Controllers/ApiController.cs) | server side api implementation |

### js ( client )

|**section**|**description**|
|---|---|
| [site.css](SecurityManagerClient/site.css) | website custom css |
| [bower.json](SecurityManagerClient/bower.json) | file populated initially with `bower init` and then with `bower install dep --save` containing js libraries dependencies |
| js-utils.js | js utils from [js-util.js](https://github.com/devel0/js-util/blob/master/src/js-util.js) |
| [utils.js](SecurityManagerClient/utils.js) | minor app utils |
| [app.js](SecurityManagerClient/app.js) | main client-side SPA app logic |
| [index.html](SecurityManagerClient/index.html) | graphics markup |

### docker ( container )

|**section**|**description**|
|---|---|
| [replace-token-with](docker/replace-token-with) | c# util to replace text in files ( sed -i makes it difficult when escaping ) |
| [config-sec0.json](docker/config-sec0.json) | example docker container config file ) |
| [run.sh](docker/run.sh) | create docker container |
| [entrypoint.sh](docker/entrypoint.sh) | every restart entry script that compile and install first time from source distro into binary and that start local web server + webapi server everytime |
