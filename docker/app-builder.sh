#!/bin/bash

echo "PATH=$PATH"
echo "DOTNET_ROOT=$DOTNET_ROOT"

echo && \
        echo "---> building web api server" && \
        echo && \
        cd /opt/securitymanager/SecurityManagerWebapi && \
        rm -fr obj bin && \
        echo "restoring..." && dotnet restore -v d && \
        echo "building..." && dotnet build -c Release .

echo && \
        echo "---> building web client" && \
        echo && \
        cd /opt/securitymanager/SecurityManagerClient && \
        bower install --allow-root
