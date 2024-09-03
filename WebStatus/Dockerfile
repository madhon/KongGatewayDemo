ARG VERSION=8.0-azurelinux3.0-distroless-extra
ARG SDKVERSION=8.0-azurelinux3.0

FROM mcr.microsoft.com/dotnet/aspnet:$VERSION AS base
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS="8080"
ENV ASPNETCORE_ENVIRONMENT="Development"
ENV DOTNET_RUNNING_IN_CONTAINER="true"
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT="0"
EXPOSE 8080/tcp

#RUN apk add --upgrade --no-cache apk-tools openssl icu-libs tzdata

#HEALTHCHECK --interval=5s --timeout=5s --retries=3 CMD wget http://127.0.0.1:80/healthz -q -O - > /dev/null 2>&1

FROM mcr.microsoft.com/dotnet/sdk:$SDKVERSION  AS build
WORKDIR /src

COPY "KongGatewayDemo.sln" "KongGatewayDemo.sln"
COPY "Directory.Packages.props" "Directory.Packages.props"
COPY "Directory.Build.props" "Directory.Build.props"
COPY "nuget.config" "nuget.config"
COPY "docker-compose.dcproj" "docker-compose.dcproj"
COPY "DC/DC.csproj" "DC/DC.csproj"
COPY "Marvel/Marvel.csproj" "Marvel/Marvel.csproj"
COPY "WebStatus/WebStatus.csproj" "WebStatus/WebStatus.csproj"

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet restore "KongGatewayDemo.sln"

COPY . .
WORKDIR /src/WebStatus
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish --no-restore -c Release -o /app/publish /p:UseAppHost=false


FROM build AS publish

FROM base AS final
USER $APP_UID
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebStatus.dll"]