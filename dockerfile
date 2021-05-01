#
# Build Stage
#

FROM mcr.microsoft.com/dotnet/sdk:3.1 as base
WORKDIR /src

COPY FiMSharp ./FiMSharp
COPY FiMSharp.Javascript ./FiMSharp.Javascript
COPY FiMSharp.Test ./FiMSharp.Test
COPY FiMSharp.sln .

RUN dotnet publish FiMSharp.Test -c Release -o /app -p:PublishSingleFile=True --runtime linux-x64

#
# Final Stage
#

FROM mcr.microsoft.com/dotnet/runtime:5.0
WORKDIR /app
COPY --from=base /app .
COPY Reports Reports/
ENTRYPOINT [ "/app/FiMSharp.Test" ]