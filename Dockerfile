#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM golang:alpine AS gobuilder
WORKDIR /go/src/github.com/k8-proxy/go-k8s-process
COPY go-k8s-process .
RUN cd cmd \
    && env GOOS=linux GOARCH=amd64 CGO_ENABLED=0 go build -o  go-k8s-process .

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim AS base
RUN apt-get update && apt-get install -y libfreetype6 fontconfig-config libc6
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src

COPY Source/Service/Service.csproj Source/Service/
COPY Source/Common/Engine/Engine.csproj Source/Common/
COPY Source/Common/Engine.Common/Engine.Common.csproj Source/Common/
COPY Source/Common/Engine.Messaging/Engine.Messaging.csproj Source/Common/

COPY lib/libs/rebuild/linux/libglasswall.classic.so lib/libs/rebuild/linux/
COPY Source/Service/libfreetype.so.6 Source/Service/libfreetype.so.6
RUN dotnet restore Source/Service/Service.csproj 

COPY . .
WORKDIR /src/Source/Service
RUN dotnet build Service.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish Service.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .

COPY adaptation /app/adaptation
COPY appsettings.json /app/config/appsettings.json

COPY --from=gobuilder /go/src/github.com/k8-proxy/go-k8s-process/cmd/go-k8s-process /app/go-k8s-process
COPY entrypoint.sh /
RUN  chmod +x /entrypoint.sh

ENTRYPOINT ["/entrypoint.sh"]
