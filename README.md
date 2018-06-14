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

## nginx config example

- `/etc/nginx/nginx.conf`

```
events
{
}

http {
        index index.html;

        # comment follow line to decrease warning log to only show errors
        error_log /var/log/nginx/error.log warn;
        
        client_max_body_size 5120M;
        proxy_connect_timeout 1600;
        proxy_send_timeout 1600;
        proxy_read_timeout 1600;
        send_timeout 1600;

        server {
                listen 80 default_server;
                listen [::]:80 default_server;
                server_name _;
                rewrite ^ https://$host$request_uri? permanent;
        }

        ssl_certificate /etc/ssl/certs/searchathing.com.crt;
        ssl_certificate_key /etc/ssl/private/searchathing.com.key;
        ssl_protocols TLSv1.2;

        server {
                listen 443 ssl default_server;
                listen [::]:443 ssl default_server;

                root /var/www/html;

                server_name searchathing.com www.searchathing.com;

                location / {
#                       try_files $uri $uri/;
                        access_log /logs/web_access.log;
                }
        }

        include /etc/nginx/conf.d/*.conf;
}
```

- /etc/nginx/conf.d/sec0.conf

```
server {
        listen 443 ssl;
        listen [::]:443 ssl;

        root /var/www/html;

        server_name sec0.searchathing.com;

        location / {
                proxy_set_header Host $host;
                proxy_pass http://10.10.0.58:80;
        }

        location ~ /Api/(?<ns>.*) {
                proxy_set_header Host $host;
                proxy_pass http://10.10.0.58:5000/Api/$ns;
                proxy_set_header X-Real-IP $remote_addr;
                proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        }
}
```

## install execution example

```sh
searchathing root@main:/opensource/devel0/securitymanager/docker# ll
total 28
drwxr-xr-x 3 root root 4096 giu 14 16:13 ./
drwxr-xr-x 7 root root 4096 giu 14 16:13 ../
-rw-r--r-- 1 root root  188 giu 14 16:13 config-sec0.json
-rw-r--r-- 1 root root  188 giu 14 16:13 config-sec1.json
-rwxr-xr-x 1 root root 1337 giu 14 16:13 entrypoint.sh*
drwxr-xr-x 2 root root 4096 giu 14 16:13 replace-token-with/
-rwxr-xr-x 1 root root 1387 giu 14 16:13 run.sh*
searchathing root@main:/opensource/devel0/securitymanager/docker# ./run.sh config-sec0.json 
container [sec0]
docker network [sec0]
container ip [10.10.0.58]
urlbase [https://sec0.searchathing.com]
dbfile [/security/sec0.json]

---> press a key to continue or ctrl+c to break



---> removing previous container if exists

sec0
sec0
579c4c5d33f93af58b6b8c30dfb6815d16c33b1ca4b6719c065c07909bb6f7b0

---> Executing entrypoint [/entrypoint.d/start.sh]

---> copying distro src to /opt


---> building replace-token-with utility


Welcome to .NET Core!
---------------------
Learn more about .NET Core: https://aka.ms/dotnet-docs
Use 'dotnet --help' to see available commands or visit: https://aka.ms/dotnet-cli-docs

Telemetry
---------
The .NET Core tools collect usage data in order to help us improve your experience. The data is anonymous and doesn't include command-line arguments. The data is collected by Microsoft and shared with the community. You can opt-out of telemetry by setting the DOTNET_CLI_TELEMETRY_OPTOUT environment variable to '1' or 'true' using your favorite shell.

Read more about .NET Core CLI Tools telemetry: https://aka.ms/dotnet-cli-telemetry

ASP.NET Core
------------
Successfully installed the ASP.NET Core HTTPS Development Certificate.
To trust the certificate run 'dotnet dev-certs https --trust' (Windows and macOS only). For establishing trust on other platforms refer to the platform specific documentation.
For more information on configuring HTTPS see https://go.microsoft.com/fwlink/?linkid=848054.
Microsoft (R) Build Engine version 15.7.179.6572 for .NET Core
Copyright (C) Microsoft Corporation. All rights reserved.

  Restoring packages for /opt/securitymanager/docker/replace-token-with/replace-token-with.csproj...
  Installing Microsoft.NETCore.DotNetAppHost 2.0.0.
  Installing Microsoft.NETCore.DotNetHostResolver 2.0.0.
  Installing NETStandard.Library 2.0.0.
  Installing Microsoft.NETCore.DotNetHostPolicy 2.0.0.
  Installing Microsoft.NETCore.App 2.0.0.
  Generating MSBuild file /opt/securitymanager/docker/replace-token-with/obj/replace-token-with.csproj.nuget.g.props.
  Generating MSBuild file /opt/securitymanager/docker/replace-token-with/obj/replace-token-with.csproj.nuget.g.targets.
  Restore completed in 6.38 sec for /opt/securitymanager/docker/replace-token-with/replace-token-with.csproj.
  replace-token-with -> /opt/securitymanager/docker/replace-token-with/bin/Debug/netcoreapp2.0/replace-token-with.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:08.64

---> setup webapi url to [https://sec0.searchathing.com]


---> building web api server

Microsoft (R) Build Engine version 15.7.179.6572 for .NET Core
Copyright (C) Microsoft Corporation. All rights reserved.

Build started 6/14/18 3:00:12 PM.

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:00.43
  Restoring packages for /opt/securitymanager/SecurityManagerWebapi/SecurityManagerWebapi.csproj...
  Restoring packages for /opt/securitymanager/SecurityManagerWebapi/SecurityManagerWebapi.csproj...
  Installing Microsoft.IdentityModel.Logging 1.1.4.
  Installing Microsoft.IdentityModel.Tokens 5.1.4.
  Installing System.IdentityModel.Tokens.Jwt 5.1.4.
  Installing System.Text.Encoding.CodePages 4.4.0.
  Installing SQLitePCLRaw.lib.e_sqlite3.osx 1.1.7.
  Installing SQLitePCLRaw.lib.e_sqlite3.v110_xp 1.1.7.
  Installing SQLitePCLRaw.lib.e_sqlite3.linux 1.1.7.
  Installing SQLitePCLRaw.provider.e_sqlite3.netstandard11 1.1.7.
  Installing Microsoft.IdentityModel.Protocols 2.1.4.
  Installing System.Security.AccessControl 4.4.0.
  Installing Microsoft.DotNet.PlatformAbstractions 2.0.3.
  Installing System.Data.SqlClient 4.4.3.
  Installing Microsoft.IdentityModel.Clients.ActiveDirectory 3.14.1.
  Installing Microsoft.CodeAnalysis.Common 2.3.1.
  Installing Microsoft.CodeAnalysis.CSharp 2.3.1.
  Installing SQLitePCLRaw.bundle_green 1.1.7.
  Installing netcore-util 1.0.0-CI00021.
  Installing Remotion.Linq 2.1.1.
  Installing Mono.Posix.NETStandard 1.0.0.
  Installing Microsoft.AspNetCore.All 2.0.6.
  Installing Microsoft.CSharp 4.4.1.
  Installing Newtonsoft.Json 10.0.3.
  Installing System.Collections.Immutable 1.4.0.
  Installing Microsoft.AspNetCore 2.0.2.
  Installing Microsoft.Extensions.FileProviders.Physical 2.0.1.
  Installing Microsoft.Extensions.FileProviders.Embedded 2.0.1.
  Installing Microsoft.IdentityModel.Protocols.OpenIdConnect 2.1.4.
  Installing Microsoft.AspNetCore.Identity 2.0.2.
  Installing Microsoft.Extensions.Hosting.Abstractions 2.0.2.
  Installing Microsoft.AspNetCore.Authentication.Core 2.0.2.
  Installing System.Reflection.Metadata 1.5.0.
  Installing Microsoft.Extensions.Options 2.0.1.
  Installing Microsoft.Extensions.ObjectPool 2.0.0.
  Installing Microsoft.Extensions.Logging.Console 2.0.1.
  Installing Microsoft.AspNetCore.Hosting.Abstractions 2.0.2.
  Installing Microsoft.AspNetCore.Hosting.Server.Abstractions 2.0.2.
  Installing Microsoft.Extensions.Logging.Abstractions 2.0.1.
  Installing Microsoft.Extensions.Localization 2.0.2.
  Installing Microsoft.AspNetCore.AzureAppServices.HostingStartup 2.0.2.
  Installing Microsoft.AspNetCore.Authentication 2.0.3.
  Installing Microsoft.EntityFrameworkCore.InMemory 2.0.2.
  Installing System.ComponentModel.Annotations 4.4.0.
  Installing Microsoft.Extensions.DependencyInjection 2.0.0.
  Installing Microsoft.AspNetCore.Http.Features 2.0.2.
  Installing Microsoft.AspNetCore.Authorization 2.0.3.
  Installing Microsoft.VisualStudio.Web.BrowserLink 2.0.2.
  Installing Microsoft.AspNetCore.Localization 2.0.2.
  Installing Microsoft.Extensions.Logging 2.0.1.
  Installing Microsoft.AspNetCore.ResponseCaching.Abstractions 2.0.2.
  Installing Microsoft.Extensions.Configuration 2.0.1.
  Installing Microsoft.AspNetCore.Mvc.Localization 2.0.3.
  Installing Microsoft.AspNetCore.ResponseCompression 2.0.2.
  Installing Microsoft.Extensions.FileProviders.Abstractions 2.0.1.
  Installing Microsoft.AspNetCore.Authentication.Google 2.0.3.
  Installing Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore 2.0.2.
  Installing Microsoft.AspNetCore.Localization.Routing 2.0.2.
  Installing Microsoft.AspNetCore.DataProtection.AzureStorage 2.0.2.
  Installing Microsoft.AspNetCore.SpaServices 2.0.3.
  Installing Microsoft.Extensions.Logging.TraceSource 2.0.1.
  Installing Microsoft.AspNetCore.Mvc 2.0.3.
  Installing Microsoft.AspNetCore.Authentication.MicrosoftAccount 2.0.3.
  Installing Microsoft.AspNetCore.Authentication.Abstractions 2.0.2.
  Installing Microsoft.Extensions.Caching.Abstractions 2.0.1.
  Installing Microsoft.Extensions.FileProviders.Composite 2.0.1.
  Installing Microsoft.Extensions.Logging.Debug 2.0.1.
  Installing Microsoft.AspNetCore.Authorization.Policy 2.0.3.
  Installing Microsoft.Extensions.FileSystemGlobbing 2.0.1.
  Installing Microsoft.Extensions.Configuration.CommandLine 2.0.1.
  Installing Microsoft.AspNetCore.Session 2.0.2.
  Installing Microsoft.AspNetCore.Authentication.Facebook 2.0.3.
  Installing Microsoft.AspNetCore.ApplicationInsights.HostingStartup 2.0.2.
  Installing Microsoft.AspNetCore.Mvc.Razor.Extensions 2.0.2.
  Installing Microsoft.Win32.Registry 4.4.0.
  Installing Microsoft.AspNetCore.HttpOverrides 2.0.2.
  Installing Microsoft.Extensions.Configuration.EnvironmentVariables 2.0.1.
  Installing Microsoft.AspNetCore.Razor 2.0.2.
  Installing Microsoft.AspNetCore.Mvc.ApiExplorer 2.0.3.
  Installing Microsoft.Extensions.Configuration.Abstractions 2.0.1.
  Installing Microsoft.Extensions.Configuration.FileExtensions 2.0.1.
  Installing Microsoft.AspNetCore.Antiforgery 2.0.2.
  Installing Microsoft.AspNetCore.Mvc.Cors 2.0.3.
  Installing Microsoft.AspNetCore.Identity.EntityFrameworkCore 2.0.2.
  Installing Microsoft.Extensions.Configuration.Ini 2.0.1.
  Installing Microsoft.Extensions.Logging.EventSource 2.0.1.
  Installing Microsoft.AspNetCore.DataProtection.Extensions 2.0.2.
  Installing Microsoft.Extensions.Caching.Redis 2.0.1.
  Installing Microsoft.AspNetCore.Routing.Abstractions 2.0.2.
  Installing Microsoft.AspNetCore.Cryptography.Internal 2.0.2.
  Installing Microsoft.AspNetCore.Mvc.Formatters.Json 2.0.3.
  Installing Microsoft.AspNetCore.CookiePolicy 2.0.3.
  Installing Microsoft.AspNetCore.DataProtection.Abstractions 2.0.2.
  Installing Microsoft.Extensions.Configuration.UserSecrets 2.0.1.
  Installing Microsoft.AspNetCore.Authentication.OAuth 2.0.3.
  Installing Microsoft.AspNetCore.AzureAppServicesIntegration 2.0.2.
  Installing Microsoft.AspNetCore.StaticFiles 2.0.2.
  Installing Microsoft.AspNetCore.Diagnostics.Abstractions 2.0.2.
  Installing Microsoft.AspNetCore.Owin 2.0.2.
  Installing Microsoft.Extensions.Configuration.Json 2.0.1.
  Installing Microsoft.AspNetCore.Authentication.Twitter 2.0.3.
  Installing Microsoft.AspNetCore.Server.Kestrel.Https 2.0.2.
  Installing Microsoft.AspNetCore.Http 2.0.2.
  Installing Microsoft.AspNetCore.Cryptography.KeyDerivation 2.0.2.
  Installing Microsoft.AspNetCore.Cors 2.0.2.
  Installing Microsoft.EntityFrameworkCore.Sqlite.Core 2.0.2.
  Installing Microsoft.Extensions.Localization.Abstractions 2.0.2.
  Installing Microsoft.Extensions.DependencyInjection.Abstractions 2.0.0.
  Installing Microsoft.AspNetCore.Server.Kestrel 2.0.2.
  Installing System.Security.Principal.Windows 4.4.0.
  Installing Microsoft.Extensions.Options.ConfigurationExtensions 2.0.1.
  Installing Microsoft.Extensions.Configuration.Binder 2.0.1.
  Installing Microsoft.AspNetCore.Authentication.Cookies 2.0.3.
  Installing Microsoft.AspNetCore.Http.Abstractions 2.0.2.
  Installing Microsoft.Extensions.WebEncoders 2.0.1.
  Installing Microsoft.AspNetCore.WebUtilities 2.0.2.
  Installing Microsoft.AspNetCore.Html.Abstractions 2.0.1.
  Installing Microsoft.AspNetCore.NodeServices 2.0.3.
  Installing Microsoft.AspNetCore.Mvc.Abstractions 2.0.3.
  Installing Microsoft.AspNetCore.Http.Extensions 2.0.2.
  Installing Microsoft.Extensions.Caching.Memory 2.0.1.
  Installing Microsoft.AspNetCore.Mvc.RazorPages 2.0.3.
  Installing Microsoft.AspNetCore.Mvc.Formatters.Xml 2.0.3.
  Installing Microsoft.AspNetCore.Rewrite 2.0.2.
  Installing Microsoft.Net.Http.Headers 2.0.2.
  Installing Microsoft.Extensions.Logging.Configuration 2.0.1.
  Installing Microsoft.AspNetCore.Razor.Runtime 2.0.2.
  Installing Microsoft.AspNetCore.Routing 2.0.2.
  Installing Microsoft.AspNetCore.Mvc.TagHelpers 2.0.3.
  Installing Microsoft.AspNetCore.Mvc.Razor.ViewCompilation 2.0.3.
  Installing Microsoft.Extensions.Configuration.Xml 2.0.1.
  Installing Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv 2.0.2.
  Installing Microsoft.AspNetCore.Mvc.ViewFeatures 2.0.3.
  Installing Microsoft.Extensions.Logging.AzureAppServices 2.0.1.
  Installing Microsoft.Extensions.DependencyModel 2.0.3.
  Installing Microsoft.CSharp 4.4.0.
  Installing Microsoft.EntityFrameworkCore.Design 2.0.2.
  Installing Microsoft.EntityFrameworkCore.Tools 2.0.2.
  Installing Microsoft.Extensions.Primitives 2.0.0.
  Installing Microsoft.AspNetCore.MiddlewareAnalysis 2.0.2.
  Installing Microsoft.Extensions.DiagnosticAdapter 2.0.1.
  Installing Microsoft.AspNetCore.WebSockets 2.0.2.
  Installing System.Threading.Tasks.Extensions 4.4.0.
  Installing Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions 2.0.2.
  Installing Microsoft.Data.Sqlite.Core 2.0.1.
  Installing Microsoft.AspNetCore.ResponseCaching 2.0.2.
  Installing SQLitePCLRaw.core 1.1.7.
  Installing Microsoft.AspNetCore.Server.Kestrel.Core 2.0.2.
  Installing Microsoft.AspNetCore.JsonPatch 2.0.0.
  Installing Microsoft.EntityFrameworkCore.Relational 2.0.2.
  Installing Microsoft.AspNetCore.Mvc.Core 2.0.3.
  Installing Microsoft.AspNetCore.Razor.Language 2.0.2.
  Installing System.Numerics.Vectors 4.4.0.
  Installing Microsoft.AspNetCore.Server.IISIntegration 2.0.2.
  Installing Microsoft.AspNetCore.Server.HttpSys 2.0.3.
  Installing Microsoft.AspNetCore.DataProtection 2.0.2.
  Installing Microsoft.AspNetCore.Mvc.DataAnnotations 2.0.3.
  Installing Microsoft.Extensions.Identity.Stores 2.0.2.
  Installing Microsoft.Extensions.Identity.Core 2.0.2.
  Installing Microsoft.AspNetCore.Diagnostics 2.0.2.
  Installing Microsoft.AspNetCore.Hosting 2.0.2.
  Installing Microsoft.AspNetCore.Authentication.JwtBearer 2.0.3.
  Installing Microsoft.AspNetCore.Authentication.OpenIdConnect 2.0.3.
  Installing Microsoft.EntityFrameworkCore 2.0.2.
  Installing System.Diagnostics.DiagnosticSource 4.4.1.
  Installing Microsoft.Data.Sqlite 2.0.1.
  Installing Microsoft.EntityFrameworkCore.Sqlite 2.0.2.
  Installing Microsoft.AspNetCore.Mvc.Razor 2.0.3.
  Installing Microsoft.CodeAnalysis.Razor 2.0.2.
  Installing System.Runtime.CompilerServices.Unsafe 4.4.0.
  Installing Microsoft.Extensions.Configuration.AzureKeyVault 2.0.1.
  Installing Microsoft.Extensions.Caching.SqlServer 2.0.1.
  Installing Microsoft.EntityFrameworkCore.SqlServer 2.0.2.
  Installing System.Text.Encodings.Web 4.4.0.
  Installing System.ValueTuple 4.4.0.
  Installing System.Buffers 4.4.0.
  Installing System.Security.Cryptography.Xml 4.4.0.
  Installing Microsoft.NETCore.DotNetAppHost 2.0.6.
  Installing System.Threading.Overlapped 4.0.1.
  Installing System.Security.Principal 4.0.1.
  Installing Microsoft.Win32.Registry 4.0.0.
  Installing Microsoft.NETCore.DotNetHostResolver 2.0.6.
  Installing System.IO.Pipes 4.0.0.
  Installing System.Threading.Tasks.Dataflow 4.6.0.
  Installing System.Diagnostics.FileVersionInfo 4.0.0.
  Installing System.Diagnostics.Contracts 4.0.1.
  Installing System.Runtime.Loader 4.0.0.
  Installing System.Threading.ThreadPool 4.0.10.
  Installing System.Reflection.Metadata 1.3.0.
  Installing System.Linq.Parallel 4.0.1.
  Installing System.Collections.Immutable 1.2.0.
  Installing System.Xml.XPath 4.0.1.
  Installing System.Xml.XPath.XmlDocument 4.0.1.
  Installing System.Resources.Writer 4.0.0.
  Installing System.Diagnostics.Process 4.1.0.
  Installing System.Runtime.Serialization.Xml 4.1.1.
  Installing System.Collections.NonGeneric 4.0.1.
  Installing System.Diagnostics.TraceSource 4.0.0.
  Installing System.Resources.Reader 4.0.0.
  Installing System.Threading.Thread 4.0.0.
  Installing NETStandard.Library 2.0.1.
  Installing Microsoft.NETCore.DotNetHostPolicy 2.0.6.
  Installing Microsoft.NETCore.Platforms 2.0.1.
  Installing Microsoft.Build 15.3.409.
  Installing Microsoft.VisualStudio.Web.CodeGeneration.Tools 2.0.3.
  Installing Microsoft.VisualStudio.Web.CodeGeneration.Contracts 2.0.3.
  Installing NuGet.Frameworks 4.0.0.
  Installing Microsoft.Build.Runtime 15.3.409.
  Installing Microsoft.NETCore.App 2.0.6.
  Installing Microsoft.Build.Framework 15.3.409.
  Installing Microsoft.Build.Tasks.Core 15.3.409.
  Installing System.Text.Encoding.CodePages 4.0.1.
  Installing Microsoft.Build.Utilities.Core 15.3.409.
  Generating MSBuild file /opt/securitymanager/SecurityManagerWebapi/obj/SecurityManagerWebapi.csproj.nuget.g.props.
  Generating MSBuild file /opt/securitymanager/SecurityManagerWebapi/obj/SecurityManagerWebapi.csproj.nuget.g.targets.
  Restore completed in 23.84 sec for /opt/securitymanager/SecurityManagerWebapi/SecurityManagerWebapi.csproj.
  Restore completed in 24.19 sec for /opt/securitymanager/SecurityManagerWebapi/SecurityManagerWebapi.csproj.
Microsoft (R) Build Engine version 15.7.179.6572 for .NET Core
Copyright (C) Microsoft Corporation. All rights reserved.

  Restore completed in 22.26 ms for /opt/securitymanager/SecurityManagerWebapi/SecurityManagerWebapi.csproj.
  Restore completed in 75.86 ms for /opt/securitymanager/SecurityManagerWebapi/SecurityManagerWebapi.csproj.
  SecurityManagerWebapi -> /opt/securitymanager/SecurityManagerWebapi/bin/Release/netcoreapp2.0/SecurityManagerWebapi.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:02.61

---> run webapi server


---> run web server


===> app ready ( ctrl+c to stop log )

[docker 579c4c5d33f9:/]# Serving at http://579c4c5d33f9:80, http://127.0.0.1:80, http://10.10.0.58:80
warn: Microsoft.AspNetCore.DataProtection.KeyManagement.XmlKeyManager[35]
      No XML encryptor configured. Key {7344b722-09a5-4c98-87e3-9e95bafa18c6} may be persisted to storage in unencrypted form.
Hosting environment: Production
Content root path: /opt/securitymanager/SecurityManagerWebapi
Now listening on: http://0.0.0.0:5000
Application started. Press Ctrl+C to shut down.
```
