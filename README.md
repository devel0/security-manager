# security manager

Webapi + webapp personal wallet cloud 

## prerequisites

- build [dotnet bionic](https://github.com/devel0/docker-dotnet/blob/bionic/README.md)

## debug

- start web server ( from SecurityManagerClient folder )

```
ws -p 80 --spa index.html
```

- start webapi server ( from vscode SecurityManagerWebapi folder using `.NET Core Launch (console)` )

- start firefox debug using `firefox --start-debugger-server`

- start web client ( from vscode SecurityManagerClient folder using `Launch localhost` )

- browse http://localhost from opened firefox debug window
