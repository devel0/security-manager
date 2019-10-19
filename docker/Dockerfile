FROM mcr.microsoft.com/dotnet/core/sdk:2.1

ENV PATH="/root/.dotnet/tools:$PATH"

# install nodejs 10
RUN curl -sL https://deb.nodesource.com/setup_10.x | bash -
RUN apt-get install -y nodejs

# install shu
RUN dotnet tool install --global shu --version 0.0.6

# install bower
RUN echo "installing local web server" && \
        npm install -g local-web-server  

# cache nuget
WORKDIR /opt/securitymanager/SecurityManagerWebapi
COPY ["src-copy/SecurityManagerWebapi/SecurityManagerWebapi.csproj", "."]
RUN dotnet restore --disable-parallel -v n .

# install bower
RUN npm install -g bower

# cache npm
WORKDIR /opt/securitymanager/SecurityManagerClient
COPY ["src-copy/SecurityManagerClient/bower.json", "."]
RUN bower install --allow-root

# srcs
WORKDIR /opt/securitymanager
COPY ["src-copy", "."]

RUN echo "---> building web api server" && \
        echo && \
        cd /opt/securitymanager/SecurityManagerWebapi && \
        rm -fr obj bin && \        
        echo "building..." && dotnet build -c Release .

RUN echo "---> building web client" && \
        echo && \
        cd /opt/securitymanager/SecurityManagerClient && \
        bower install --allow-root

RUN echo && \        
        URLBASE=$(cat urlbase) && \
        echo "---> setup webapi url to [$URLBASE]" && \
        echo && \
        appjs=/opt/securitymanager/SecurityManagerClient/app.js && \
        cat "$appjs" | \
                shu replace-token "urlbase = 'http://localhost:5000'" "urlbase = '$URLBASE'" | \
                shu replace-token "debugmode = true;" "debugmode = false;" \
                > /tmp/x && mv -f /tmp/x "$appjs"

COPY entrypoint.sh /root

ENTRYPOINT [ "/root/entrypoint.sh" ]

CMD /bin/bash
