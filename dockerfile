# Compile Project
FROM mcr.microsoft.com/dotnet/sdk:3.1

WORKDIR /src

COPY FiMSharp.Test .

RUN dotnet restore

RUN dotnet publish -c Release -o /app -p:PublishSingleFile=True --runtime linux-x64

WORKDIR /app

COPY Reports Reports/

ENTRYPOINT [ "/app/FiMSharp.Test" ]